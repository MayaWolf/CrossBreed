using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using CrossBreed.ViewModels;

namespace CrossBreed.Windows.Views {
	public partial class LogsView {
		private bool isReset = true;
		public LogsView() {
			DataContextChanged += (_, __) => {
				var messages = ViewModel.Messages;
				foreach(var message in messages.ToListAndRegister(MessageCollectionChanged)) {
					TextView.Document.Blocks.Add(new Paragraph(BBCode.ToInlines(message.Formatted)) { Margin = new Thickness(0, 0, 0, App.Current.Theme.FontSize / 3) });
				}
			};
			InitializeComponent();
		}


		private void MessageCollectionChanged(object sender, NotifyCollectionChangedEventArgs args) {
			IEnumerable<MessageViewModel> messages;
			switch(args.Action) {
				case NotifyCollectionChangedAction.Add:
					messages = args.NewItems.Cast<MessageViewModel>();
					break;
				case NotifyCollectionChangedAction.Reset:
					messages = ViewModel.Messages.ToList();
					isReset = true;
					TextView.Document = new FlowDocument();
					break;
				default:
					return;
			}
			var blocks = TextView.Document.Blocks;
			var block = CreateBlock(messages.First());
			if(blocks.FirstBlock != null) blocks.InsertBefore(blocks.FirstBlock, block);
			else blocks.Add(block);
			foreach(var message in messages) blocks.InsertAfter(block, block = CreateBlock(message));
		}

		private static Paragraph CreateBlock(MessageViewModel message) =>
			new Paragraph(BBCode.ToInlines(message.Formatted)) { Margin = new Thickness(0, 0, 0, App.Current.Theme.FontSize / 3) };

		private void ScrollChanged(object sender, ScrollChangedEventArgs e) {
			if(e.VerticalOffset < 1 && e.VerticalChange != 0) ViewModel.LoadMore();
			else if(e.ViewportHeightChange != 0) {
				var scrollViewer = (ScrollViewer) sender;
				if(isReset) {
					scrollViewer.ScrollToEnd();
					isReset = false;
				} else scrollViewer.ScrollToVerticalOffset(e.ExtentHeightChange);
			}
		}
	}
}