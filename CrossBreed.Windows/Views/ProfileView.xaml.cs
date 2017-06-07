using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CodeKicker.BBCode.SyntaxTree;
using CrossBreed.ViewModels;
using CrossBreed.Chat;

namespace CrossBreed.Windows.Views {
	public partial class ProfileView {
		public ProfileViewModel Profile { get; private set; }

		public ProfileView() {
			DataContextChanged += OnViewModelSet;
		}

		private async void OnViewModelSet(object sender, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs) {
			Profile = await ViewModel.GetProfile();
			InitializeComponent();
			AddProfile(Profile.Description.SubNodes, Document.Blocks, null, null);
		}

		private void AddProfile(ISyntaxTreeNodeCollection nodes, BlockCollection blocks, Paragraph currentParagraph, Span currentSpan) {
			foreach(var node in nodes) {
				var tagNode = node as TagNode;
				if(tagNode != null) {
					var parameter = tagNode.AttributeValues.Values.FirstOrDefault();
					switch(tagNode.Tag.Name) {
						case "left":
						case "center":
						case "right":
						case "justify":
							var aligned = new Section { TextAlignment = tagNode.Tag.Name.ToEnum<TextAlignment>() };
							blocks.Add(aligned);
							AddProfile(tagNode.SubNodes, aligned.Blocks, null, null);
							currentParagraph = null;
							continue;
						case "collapse":
							var document = new FlowDocument { FontFamily = Document.FontFamily, FontSize = Document.FontSize, Foreground = Document.Foreground };
							var viewer = new FlowDocumentScrollViewer { Document = document, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled };
							viewer.PreviewMouseWheel += (sender, args) => {
								args.Handled = true;
								DocumentViewer.RaiseEvent(new MouseWheelEventArgs(args.MouseDevice, args.Timestamp, args.Delta) { RoutedEvent = MouseWheelEvent, Source = sender });
							};
							blocks.Add(new BlockUIContainer(new Expander { Content = viewer, Header = parameter }));
							AddProfile(node.SubNodes, document.Blocks, null, null);
							currentParagraph = null;
							continue;
						case "indent":
							var indented = new Section { Margin = new Thickness(10, 0, 0, 0) };
							blocks.Add(indented);
							AddProfile(tagNode.SubNodes, indented.Blocks, null, null);
							currentParagraph = null;
							continue;
						case "hr":
							blocks.Add(new BlockUIContainer(new Separator()));
							currentParagraph = null;
							continue;
						case "heading":
							blocks.Add(new Paragraph { FontSize = 18, FontWeight = FontWeights.Bold, Inlines = { BBCode.ToInlines(node.SubNodes) } });
							currentParagraph = null;
							continue;
						case "img":
							var image = new BitmapImage(new Uri(Profile.InlineImages[int.Parse(parameter)].Url));
							blocks.Add(new BlockUIContainer(new Image { Source = image, Stretch = Stretch.None }));
							currentParagraph = null;
							continue;
					}
				}
				if(currentParagraph == null) {
					currentParagraph = new Paragraph();
					currentSpan = new Span();
					blocks.Add(currentParagraph);
					currentParagraph.Inlines.Add(currentSpan);
				}
				if(tagNode != null) {
					var inline = BBCode.ToInline(tagNode);
					currentSpan.Inlines.Add(inline);
					var span = inline as Span;
					if(span != null) AddProfile(tagNode.SubNodes, blocks, currentParagraph, span);
				} else currentSpan.Inlines.Add(new Run(node.ToText()));
			}
		}
	}
}