using System.Windows;
using System.Windows.Controls;
using CodeKicker.BBCode.SyntaxTree;
using ML.DependencyProperty;

namespace CrossBreed.Windows.Views {
	public class BbCodeView : TextBlock {
		[DependencyProperty(ChangedCallback = nameof(BbCodeChanged))]
		public SyntaxTreeNode BbCode { get; set; }

		private void BbCodeChanged(DependencyPropertyChangedEventArgs args) {
			Inlines.Clear();
			if(args.NewValue == null) return;
			Inlines.Add(BBCode.ToInlines((SyntaxTreeNode) args.NewValue));
		}
	}
}