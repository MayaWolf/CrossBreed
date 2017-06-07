using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CodeKicker.BBCode.SyntaxTree;
using CrossBreed.Chat;
using CrossBreed.ViewModels;
using Microsoft.Win32;
using MvvmCross.Platform;
using WpfAnimatedGif;

namespace CrossBreed.Windows {
	public static class BBCode {
		public static Inline ToInlines(SyntaxTreeNode node, Func<TagNode, Inline> customMapper = null) => Convert(node, customMapper);

		public static Inline ToInlines(ISyntaxTreeNodeCollection nodes, Func<TagNode, Inline> customMapper = null) {
			var span = new Span();
			foreach(var node in nodes) span.Inlines.Add(Convert(node, customMapper));
			return span;
		}

		private static Inline Convert(SyntaxTreeNode node, Func<TagNode, Inline> customMapper) {
			if(node is TextNode) return new Run(node.ToText());
			Span span;
			if(node is TagNode) {
				var custom = customMapper?.Invoke((TagNode) node);
				if(custom != null) return custom;
				var inline = ToInline((TagNode) node);
				span = inline as Span;
				if(span == null) return inline;
			} else span = new Span();
			foreach(var inline in node.SubNodes) span.Inlines.Add(Convert(inline, customMapper));

			return span;
		}

		private static Span CreateSpan(TextPointer start = null, TextPointer end = null) {
			if(start != null && end != null) return new Span(start, end);
			return start == null ? new Span() : new Span((Inline) null, start);
		}

		public static Inline ToInline(TagNode node, TextPointer start = null, TextPointer end = null) {
			var parameter = node.AttributeValues.Count > 0 ? node.AttributeValues.Values.First() : null;
			switch(node.Tag.Name) {
				case "b":
					return new Bold();
				case "i":
					return new Italic();
				case "u":
					return new Underline();
				case "s":
					return new Span { TextDecorations = TextDecorations.Strikethrough };
				case "sub":
					return new Span { BaselineAlignment = BaselineAlignment.Subscript, FontSize = 9 };
				case "sup":
					return new Span { BaselineAlignment = BaselineAlignment.Superscript, FontSize = 9 };
				case "url":
					parameter = parameter ?? node.ToText();
					var copyItem = new MenuItem { Header = Strings.Chat_CopyLink };
					copyItem.Click += (sender, args) => Clipboard.SetText(parameter);
					var incognitoItem = new MenuItem { Header = Strings.Chat_OpenIncognito };
					incognitoItem.Click += (sender, args) => {
						using(var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice")) {
							switch(key?.GetValue("Progid")?.ToString()) {
								case "FirefoxURL":
									Process.Start("firefox.exe", "-private-window " + parameter);
									break;
								case "ChromeHTML":
									Process.Start("chrome.exe", "-incognito " + parameter);
									break;
								default:
									Process.Start("iexplore.exe", "-private " + parameter);
									break;
							}
						}
					};
					var link = new Hyperlink { NavigateUri = new Uri(parameter), ToolTip = parameter, ContextMenu = new ContextMenu { Items = { copyItem, incognitoItem } } };
					link.RequestNavigate += (sender, e) => {
						if(UserSettings.Instance.General.AlwaysIncognito) incognitoItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
						else Process.Start(e.Uri.ToString());
					};
					return link;
				case "big":
					return new Span { FontSize = App.Current.Theme.FontSizeBig };
				case "small":
					return new Span { FontSize = App.Current.Theme.FontSizeSmall };
				case "color":
					return new Span { Foreground = GetColor(parameter) };
				case "icon":
					return new InlineUIContainer(new Image {
						Source = new BitmapImage(new Uri(CharacterViewModel.GetAvatar(node.ToText()))),
						Height = 50,
						Width = 50
					}) { BaselineAlignment = BaselineAlignment.Center };
				case "eicon":
					var source = new BitmapImage(new Uri($"https://static.f-list.net/images/eicon/{node.ToText().ToLower()}.gif"));
					var image = new Image { Height = 50, Width = 50 };
					ImageBehavior.SetAnimatedSource(image, source);
					return new InlineUIContainer(image);
				case "session": //TODO channellistitem
					var session = new Hyperlink {
						Inlines = { new InlineUIContainer(new Path { Data = Icons.Channel, Fill = App.Current.Theme.Foreground, Margin = new Thickness(0, 0, 5, 0) }) },
						NavigateUri = new Uri("http://x.com")
					};
					var id = node.ToText();
					session.RequestNavigate += (sender, args) => Mvx.GetSingleton<IChannelManager>().JoinChannel(id);
					node.SubNodes.Clear();
					node.SubNodes.Add(new TextNode(parameter));
					return session;
				case "user":
					return new InlineUIContainer(new Views.UserView {
						Character = Mvx.GetSingleton<CharacterViewModels>().GetCharacterViewModel(node.ToText())
					});
			}
			return new Span();
		}

		private static Brush GetColor(string name) {
			switch(name) {
				case "red":
					return Brushes.Red;
				case "blue":
					return Brushes.Blue;
				case "white":
					return Brushes.White;
				case "pink":
					return Brushes.DeepPink;
				case "gray":
					return Brushes.Gray;
				case "green":
					return Brushes.Green;
				case "orange":
					return Brushes.Orange;
				case "purple":
					return Brushes.Purple;
				case "black":
					return Brushes.Black;
				case "brown":
					return Brushes.Brown;
				case "cyan":
					return Brushes.Cyan;
				case "yellow":
					return Brushes.Yellow;
				default:
					return (SolidColorBrush) App.Current.FindResource("Theme.Foreground");
			}
		}
	}
}