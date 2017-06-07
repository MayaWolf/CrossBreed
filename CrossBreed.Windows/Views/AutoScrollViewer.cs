using System.Windows.Controls;

namespace CrossBreed.Windows.Views {
	public class AutoScrollViewer: ScrollViewer {
		private bool autoScroll = true;

		protected override void OnScrollChanged(ScrollChangedEventArgs e) {
			base.OnScrollChanged(e);
			if(e.ExtentHeightChange == 0) {
				autoScroll = VerticalOffset == ScrollableHeight;
			} else if(autoScroll) {
				ScrollToVerticalOffset(ExtentHeight);
			}
		}
	}
}
