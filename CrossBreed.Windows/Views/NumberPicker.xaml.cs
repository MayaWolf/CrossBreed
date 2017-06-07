using ML.DependencyProperty;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace CrossBreed.Windows.Views {
	public partial class NumberPicker {
		[DependencyProperty]
		public int Value { get; set; }

		[DependencyProperty]
		public int Maximum { get; set; }

		[DependencyProperty]
		public int Minimum { get; set; }

		public NumberPicker() {
			InitializeComponent();
		}

		private void OnIncrement(object sender, RoutedEventArgs e) {
			Value += 1;
			if(Value == Maximum) ((FrameworkElement) sender).IsEnabled = false;
		}

		private void OnDecrement(object sender, RoutedEventArgs e) {
			Value -= 1;
			if(Value == Minimum) ((FrameworkElement) sender).IsEnabled = false;
		}

		private void PreviewTextInput(object sender, TextCompositionEventArgs e) {
			if(!Regex.IsMatch("^-?[0-9]+$", e.Text)) e.Handled = true;
		}
	}
}