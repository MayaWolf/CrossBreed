using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.Graphics.Drawable;
using Android.Text;
using Android.Text.Style;
using Android.Widget;
using CodeKicker.BBCode.SyntaxTree;
using CrossBreed.Chat;
using CrossBreed.Entities;
using CrossBreed.ViewModels;
using ML.AppBase;
using ML.Droid.Base;
using MvvmCross.Binding.Droid.Target;
using MvvmCross.Platform;
using MvvmCross.Plugins.DownloadCache;
using ClickableSpan = ML.Droid.Base.ClickableSpan;

namespace CrossBreed.Droid {
	class BBCodeBinding : MvxAndroidTargetBinding {
		public BBCodeBinding(TextView target) : base(target) {}
		public override Type TargetType => typeof(TextView);

		private static readonly Lazy<Drawable> sessionIcon = new Lazy<Drawable>(() => {
			var context = Application.Context;
			var drawable = VectorDrawableCompat.Create(context.Resources, Resource.Drawable.ic_dashboard_black_24dp, context.Theme);
			drawable.SetBounds(0, 0, drawable.IntrinsicWidth, drawable.IntrinsicHeight);
			return drawable;
		});

		public static async Task<SpannableStringBuilder> GetFormatted(SyntaxTreeNode node, Func<TagNode, SpannableStringBuilder, Task<bool>> customHandler = null) {
			var builder = new SpannableStringBuilder();
			await Format(node, builder, customHandler);
			return builder;
		}

		private static async Task Format(SyntaxTreeNode parent, SpannableStringBuilder builder, Func<TagNode, SpannableStringBuilder, Task<bool>> customHandler) {
			foreach(var node in parent.SubNodes) {
				var tagNode = node as TagNode;
				if(tagNode != null) {
					if(await ApplyCustomHandlers(builder, tagNode, customHandler)) continue;
					var start = builder.Length();
					await Format(tagNode, builder, customHandler);
					var end = builder.Length();
					if(start > end) start = end;
					var parameter = tagNode.AttributeValues.Count > 0 ? tagNode.AttributeValues.Values.First() : null;
					switch(tagNode.Tag.Name) {
						case "b":
							builder.SetSpan(new StyleSpan(TypefaceStyle.Bold), start, end, 0);
							break;
						case "i":
							builder.SetSpan(new StyleSpan(TypefaceStyle.Italic), start, end, 0);
							break;
						case "u":
							builder.SetSpan(new UnderlineSpan(), start, end, 0);
							break;
						case "s":
							builder.SetSpan(new StrikethroughSpan(), start, end, 0);
							break;
						case "sub":
							builder.SetSpan(new SubscriptSpan(), start, end, 0);
							break;
						case "sup":
							builder.SetSpan(new SuperscriptSpan(), start, end, 0);
							break;
						case "big":
							builder.SetSpan(new RelativeSizeSpan(1.2f), start, end, 0);
							break;
						case "small":
							builder.SetSpan(new RelativeSizeSpan(0.8f), start, end, 0);
							break;
						case "url":
							if(node.ToText() == "") {
								builder.Append(parameter);
								end = builder.Length();
							}
							builder.SetSpan(new URLSpan(parameter), start, end, 0);
							break;
						case "color":
							switch(parameter) {
								case "orange":
									parameter = "#ff9900";
									break;
								case "purple":
									parameter = "magenta";
									break;
								case "pink":
									parameter = "#ff99ff";
									break;
								case "brown":
									parameter = "#997711";
									break;
								case "grey":
									parameter = "gray";
									break;
								case "white":
									builder.SetSpan(new OutlineSpan(), start, end, 0);
									break;
							}
							try {
								builder.SetSpan(new ForegroundColorSpan(Color.ParseColor(parameter)), start, end, 0);
							} catch { }
							break;
					}
				} else builder.Append(node.ToText());
			}
		}

		private static async Task<bool> ApplyCustomHandlers(SpannableStringBuilder builder, TagNode node, Func<TagNode, SpannableStringBuilder, Task<bool>> customHandler) {
			if(customHandler != null && await customHandler(node, builder)) return true;
			var start = builder.Length();
			var parameter = node.AttributeValues.Count > 0 ? node.AttributeValues.Values.First() : null;
			switch(node.Tag.Name) {
				case "session":
					builder.Append(" " + parameter);
					builder.SetSpan(new ImageSpan(sessionIcon.Value), start, start + 1, SpanTypes.MarkMark);
					builder.SetSpan(new ClickableSpan(view => {
						var channel = parameter;
						var channelManager = Mvx.GetSingleton<IChannelManager>();
						if(channelManager.JoinedChannels.ContainsKey(channel)) {
							ViewChannel(channel);
							return;
						}
						NotifyCollectionChangedEventHandler listener = null;
						listener = (sender, args) => {
							if(args.Action != NotifyCollectionChangedAction.Add || args.NewItems.Cast<Channel>().All(x => x.Name != channel)) return;
							ViewChannel(channel);
							channelManager.JoinedChannels.CollectionChanged -= listener;
						};
						channelManager.JoinedChannels.CollectionChanged += listener;
						channelManager.JoinChannel(channel);
					}), start + 1, builder.Length(), 0);
					return true;
				case "icon":
				case "eicon":
					builder.Append(' ');
					var icon = node.Tag.Name == "icon" ? Helpers.GetAvatar(node.ToText()) : $"https://static.f-list.net/images/eicon/{node.ToText()}.gif";
					var image = new BitmapDrawable(await Mvx.GetSingleton<IMvxImageCache<Bitmap>>().RequestImage(icon));
					var size = MLHelpers.DpToPixelsInt(40);
					image.SetBounds(0, 0, size, size);
					builder.SetSpan(new ImageSpan(image), start, start + 1, 0);
					return true;
				case "user":
					var name = node.ToText();
					builder.Append(" " + name);
					var character = Mvx.GetSingleton<ICharacterManager>().GetCharacter(name);
					builder.SetSpan(new ImageSpan(StatusIconConverter.GetDrawable(character.Status)), start, start + 1, SpanTypes.MarkMark);
					builder.SetSpan(new PureClickableSpan(view => {
						Mvx.GetSingleton<IViewModelNavigator>().Show<CharacterViewModel, string>(character.Name);
					}), start, builder.Length(), 0);
					builder.SetSpan(new ForegroundColorSpan(new Color(CharacterViewModel.GetColor(character).ARGB)), start + 1, builder.Length(), 0);
					if(character.IsBookmarked || character.IsFriend) {
						builder.SetSpan(new ColoredUnderlineSpan(unchecked((int) 0xff00cc00)), start + 1, builder.Length(), 0);
					}
					return true;
				default:
					return false;
			}
		}

		protected override async void SetValueImpl(object target, object value) {
			var message = value as SyntaxTreeNode;
			if(message == null) return;
			((TextView) target).TextFormatted = await GetFormatted(message);
		}

		private static void ViewChannel(string channel) {
			Mvx.GetSingleton<IViewModelNavigator>().Show<ChatViewModel, ChatViewModel.InitArgs>(new ChatViewModel.InitArgs { Channel = channel });
		}
	}
}