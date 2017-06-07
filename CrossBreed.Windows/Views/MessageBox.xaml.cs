using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CodeKicker.BBCode;
using CodeKicker.BBCode.SyntaxTree;
using CrossBreed.Entities;
using CrossBreed.ViewModels;
using ML.Collections;
using MvvmCross.Platform;
using BindingFlags = System.Reflection.BindingFlags;
using ML.DependencyProperty;

namespace CrossBreed.Windows.Views {
	public partial class MessageBox {
		private string bbCode;
		private bool isUrlPaste;
		private readonly App.ThemeResources theme = App.Current.Theme;
		private static readonly MethodInfo insertInlineMethod = typeof(TextPointer).GetMethod("InsertInline", BindingFlags.NonPublic | BindingFlags.Instance);
		
		[DependencyProperty(ChangedCallback = nameof(OnTabViewModelSet))]
		public TabViewModel TabViewModel { get; set; }

		[DependencyProperty]
		public bool IsSimple { get; set; }

		public IReadOnlyDictionary<Color, string> AvailableColors { get; }
		public ChannelsViewModel ChannelsViewModel { get; }


		public MessageBox() {
			ChannelsViewModel = Mvx.IocConstruct<ChannelsViewModel>();
			AvailableColors = new Dictionary<Color, string> {
				{ Colors.Red, "red" }, { Colors.Blue, "blue" }, { Colors.Yellow, "yellow" }, { Colors.Green, "green" }, { Colors.Pink, "pink" }, { Colors.Gray, "gray" },
				{ Colors.Orange, "orange" }, { Colors.Purple, "purple" }, { Colors.Brown, "brown" }, { Colors.Cyan, "cyan" }, { Colors.Black, "black" }, { Colors.White, "white" },
				{ Colors.Transparent, "" }
			};
			InitializeComponent();
			DataObject.AddPastingHandler(RichTextBox, HandlePaste);
			RichTextBox.Foreground = new SolidColorBrush(Color.FromArgb(255, 1, 1, 1));
			RichTextBox.TextChanged += (sender, args) => OnTextChanged(args);
			RichTextBox.LostFocus += TextBoxFocusLost;
		}

		private void TextBoxFocusLost(object sender, RoutedEventArgs args) {
			UpdateSource(true);
		}

		private void OnTabViewModelSet(DependencyPropertyChangedEventArgs args) {
			var oldValue = (TabViewModel) args.OldValue;
			var newValue = (TabViewModel) args.NewValue;
			if(oldValue != null) {
				oldValue.EnteredTextChanged -= BbCodeChanged;
				var oldConversation = oldValue as ConversationViewModel;
				if(oldConversation != null) oldConversation.MaxMessageBytesChanged += MaxBytesSet;
				var oldChannel = oldValue as ChannelConversationViewModel;
				if(oldChannel != null) oldChannel.IsAutoPostingChanged -= AutoPostAdsChanged;
			}

			newValue.EnteredTextChanged += BbCodeChanged;
			var newConversation = newValue as ConversationViewModel;
			if(newConversation != null) newConversation.MaxMessageBytesChanged += MaxBytesSet;
			var newChannel = newValue as ChannelConversationViewModel;
			if(newChannel != null) newChannel.IsAutoPostingChanged += AutoPostAdsChanged;
		}

		private void AutoPostAdsChanged() {
			var isAutoPosting = ((ChannelConversationViewModel) TabViewModel).IsAutoPosting;
			RichTextBox.IsEnabled = !isAutoPosting;
			TextBox.IsEnabled = !isAutoPosting;
		}

		private void MaxBytesSet() {
			CharacterCount.Text = $"{(bbCode == null ? 0 : Encoding.UTF8.GetByteCount(bbCode))}/{((ConversationViewModel) TabViewModel).MaxMessageBytes}";
		}

		private void HandlePaste(object sender, DataObjectPastingEventArgs args) {
			if(args.DataObject.GetDataPresent(DataFormats.UnicodeText)) {
				var data = (string) args.DataObject.GetData(DataFormats.UnicodeText);
				Uri uri;
				if(UserSettings.Instance.General.AutoLink && Uri.TryCreate(data, UriKind.Absolute, out uri) && uri.IsWellFormedOriginalString()) {
					var path = uri.Host + uri.LocalPath;
					if(path.StartsWith("www.")) path = path.Substring(4);
					args.DataObject = new DataObject(DataFormats.UnicodeText, path.StartsWith("f-list.net/c/") ? $"[user]{path.Substring(13)}[/user]" : $"[url={data}][/url]");
					isUrlPaste = true;
				}
			}
		}

		private void BbCodeChanged() {
			if(TabViewModel.EnteredText == bbCode) return;
			RichTextBox.Document.Blocks.Clear();
			bbCode = TabViewModel.EnteredText;
			if(string.IsNullOrEmpty(bbCode)) return;
			RichTextBox.Document.Blocks.Add(new Paragraph(BBCode.ToInlines(BbCodeParser.Parse(bbCode))));
		}

		protected void OnTextChanged(TextChangedEventArgs e) {
			if(isUrlPaste) {
				RichTextBox.CaretPosition = RichTextBox.CaretPosition.GetPositionAtOffset(-6);
				isUrlPaste = false;
			}
			var start = RichTextBox.Document.ContentStart.GetNextInsertionPosition(LogicalDirection.Forward);
			var end = RichTextBox.Document.ContentEnd.GetNextInsertionPosition(LogicalDirection.Backward);
			(TabViewModel as CharacterConversationViewModel)?.TypingCommand.Execute(start != null && end != null && start.CompareTo(end) != 0);
		}

		private void UpdateSource(bool fromRich = false) {
			if(IsSimple && !fromRich) bbCode = TextBox.Text;
			else {
				var builder = new StringBuilder();
				var paragraph = RichTextBox.Document.Blocks.FirstBlock as Paragraph;
				if(paragraph != null) {
					ParseInlines(builder, paragraph.Inlines);
					bbCode = builder.ToString();
				} else bbCode = "";
			}
			TabViewModel.EnteredText = bbCode;
			var conversation = TabViewModel as ConversationViewModel;
			CharacterCount.Text = conversation == null ? "" : $"{Encoding.UTF8.GetByteCount(bbCode)}/{conversation.MaxMessageBytes}";
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e) {
			if(e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0) {
				e.Handled = true;
				UpdateSource();
				TabViewModel.SendCommand.Execute(null);
			} else base.OnPreviewKeyDown(e);
		}

		private void ParseInlines(StringBuilder builder, InlineCollection inlines) {
			foreach(var inline in inlines) {
				if(inline is LineBreak) {
					builder.Append("\n");
					continue;
				}
				var container = inline as InlineUIContainer;
				if(container != null) {
					var userView = container.Child as UserView;
					if(userView != null) builder.Append($"[user]{userView.Character.Character.Name}[/user]");
					continue;
				}
				var customTag = inline.Tag != null;
				ICollection<string> tags = new LinkedList<string>();
				if(inline.FontWeight > FontWeights.Normal) tags.Add("b");
				if(inline.FontStyle == FontStyles.Italic) tags.Add("i");
				if(inline.TextDecorations.Any(x => x.Location == TextDecorationLocation.Underline)) tags.Add("u");
				if(inline.TextDecorations.Any(x => x.Location == TextDecorationLocation.Strikethrough)) tags.Add("s");
				if(inline.BaselineAlignment == BaselineAlignment.Subscript) tags.Add("sub");
				if(inline.BaselineAlignment == BaselineAlignment.Superscript) tags.Add("sup");
				if(inline is Hyperlink && !customTag) tags.Add("url=" + ((Hyperlink) inline).NavigateUri);
				if(inline.FontSize == theme.FontSizeBig) tags.Add("big");
				if(inline.FontSize == theme.FontSizeSmall) tags.Add("small");
				var color = inline.Foreground as SolidColorBrush;
				if(color != null && AvailableColors.ContainsKey(color.Color)) tags.Add("color=" + AvailableColors[color.Color]);
				foreach(var tag in tags) builder.Append("[" + tag + "]");

				if(customTag) builder.Append(inline.Tag);
				else {
					var run = inline as Run;
					if(run != null) builder.Append(run.Text);
					else {
						var span = inline as Span;
						if(span != null) ParseInlines(builder, span.Inlines);
					}
				}

				foreach(var tag in tags.Reverse()) {
					var index = tag.IndexOf("=");
					builder.Append("[/" + (index > 0 ? tag.Substring(0, index) : tag) + "]");
				}
			}
		}

		private void HyperlinkButton(object sender, RoutedEventArgs e) {
			HyperlinkPopup.IsOpen = true;
		}

		private void HyperlinkApplyButton(object sender, RoutedEventArgs e) {
			var url = HyperlinkUrl.Text;
			if(!string.IsNullOrWhiteSpace(url)) AddTag("url", url);
		}

		private void ColorSelected(Color color) {
			if(!IsSimple) {
				if(color == Colors.Transparent) RichTextBox.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, DependencyProperty.UnsetValue);
				else RichTextBox.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
				return;
			}
			if(color != Colors.Transparent) AddTag("color", AvailableColors[color]);
		}

		private void ChannelButton(object sender, RoutedEventArgs e) {
			ChannelsPopup.IsOpen = true;
		}

		private void ChannelSelected(object sender, RoutedEventArgs e) {
			var item = (ChannelListItem) ((FrameworkElement) sender).Tag;
			var content = $"[session={item.Name}]{item.Id}[/session]";
			if(IsSimple) TextBox.SelectedText = content;
			else {
				ReplaceSelection(new Hyperlink {
					Inlines = {
						new InlineUIContainer(new Path { Data = Icons.Channel, Fill = Brushes.Black, Margin = new Thickness(0, 0, 5, 0) }),
						new Run(item.Name)
					},
					NavigateUri = new Uri("http://x.com"),
					Tag = content
				});
			}
		}

		private void BbCodeButton(object sender, RoutedEventArgs e) {
			AddTag((string) ((FrameworkElement) sender).Tag);
		}

		private void ToggleTextDecoration(TextDecorationLocation location) {
			var value = RichTextBox.Selection.GetPropertyValue(Inline.TextDecorationsProperty) as TextDecorationCollection;
			if(value == null) {
				RichTextBox.Selection.ApplyPropertyValue(Inline.TextDecorationsProperty, new TextDecorationCollection());
				return;
			}
			var collection = value.CloneCurrentValue();
			var existing = collection.FirstOrDefault(x => x.Location == location);
			if(existing != null) collection.Remove(existing);
			else collection.Add(new TextDecoration { Location = location });
			RichTextBox.Selection.ApplyPropertyValue(Inline.TextDecorationsProperty, collection);
		}

		private Paragraph GetSelectedParagraph() {
			var paragraph = RichTextBox.Selection.Start.Paragraph;
			if(paragraph == null) {
				paragraph = new Paragraph();
				RichTextBox.Document.Blocks.Add(paragraph);
			}
			return paragraph;
		}

		private void ReplaceSelection(Inline inline) {
			var selection = RichTextBox.Selection;
			selection.Start.DeleteTextInRun(selection.Start.GetOffsetToPosition(selection.End));
			insertInlineMethod.Invoke(selection.Start, new object[] { inline });
		}

		private void AddTag(string tag, string attribute = null) {
			if(IsSimple) TextBox.SelectedText = (string.IsNullOrEmpty(attribute) ? $"[{tag}]" : $"[{tag}={attribute}]") + $"{TextBox.SelectedText}[/{tag}]";
			else {
				var selection = RichTextBox.Selection;
				bool isNormal;
				switch(tag) {
					case "b":
						isNormal = selection.GetPropertyValue(TextElement.FontWeightProperty) as FontWeight? == FontWeights.Normal;
						selection.ApplyPropertyValue(TextElement.FontWeightProperty, isNormal ? FontWeights.Bold : FontWeights.Normal);
						break;
					case "i":
						isNormal = selection.GetPropertyValue(TextElement.FontStyleProperty) as FontStyle? == FontStyles.Normal;
						selection.ApplyPropertyValue(TextElement.FontStyleProperty, isNormal ? FontStyles.Italic : FontStyles.Normal);
						break;
					case "u":
						ToggleTextDecoration(TextDecorationLocation.Underline);
						break;
					case "s":
						ToggleTextDecoration(TextDecorationLocation.Strikethrough);
						break;
					case "big":
						isNormal = selection.GetPropertyValue(TextElement.FontSizeProperty) as double? == theme.FontSize;
						selection.ApplyPropertyValue(TextElement.FontSizeProperty, isNormal ? theme.FontSizeBig : theme.FontSize);
						break;
					case "small":
						isNormal = selection.GetPropertyValue(TextElement.FontSizeProperty) as double? == theme.FontSize;
						selection.ApplyPropertyValue(TextElement.FontSizeProperty, isNormal ? theme.FontSizeSmall : theme.FontSize);
						break;
					case "sup":
						isNormal = selection.GetPropertyValue(Inline.BaselineAlignmentProperty) as BaselineAlignment? == BaselineAlignment.Baseline;
						selection.ApplyPropertyValue(Inline.BaselineAlignmentProperty, isNormal ? BaselineAlignment.TextTop : BaselineAlignment.Baseline);
						selection.ApplyPropertyValue(TextElement.FontSizeProperty, isNormal ? theme.FontSizeSmall : theme.FontSize);
						break;
					case "sub":
						isNormal = selection.GetPropertyValue(Inline.BaselineAlignmentProperty) as BaselineAlignment? == BaselineAlignment.Baseline;
						selection.ApplyPropertyValue(Inline.BaselineAlignmentProperty, isNormal ? BaselineAlignment.Subscript : BaselineAlignment.Baseline);
						selection.ApplyPropertyValue(TextElement.FontSizeProperty, isNormal ? theme.FontSizeSmall : theme.FontSize);
						break;
					case "url":
						GetSelectedParagraph().Inlines.Add(new Hyperlink(selection.Start, selection.End) { NavigateUri = new Uri(attribute), IsEnabled = true });
						break;
					case "user":
					case "icon":
					case "eicon":
						var content = selection.Text.Trim();
						var inline = BBCode.ToInline(new TagNode(new BBTag(tag), new TextNode(content).SingletonEnumerable()));
						inline.Tag = $"[{tag}]{content}[/{tag}]";
						ReplaceSelection(inline);
						break;
				}
			}
		}
	}
}