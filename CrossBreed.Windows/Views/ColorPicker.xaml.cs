using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ML.DependencyProperty;

namespace CrossBreed.Windows.Views {
	public partial class ColorPicker {
		[DependencyProperty(MetadataOptions = FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)]
		public Color SelectedColor { get; set; }

		private static readonly IReadOnlyCollection<Color> colors = typeof(Colors).GetProperties().Select(x => (Color) x.GetValue(null)).ToList();

		[DependencyProperty]
		public IReadOnlyCollection<Color> AvailableColors { get; set; } = colors;

		[DependencyProperty]
		public Style ButtonStyle { get; set; }


		public event Action<Color> SelectedColorChanged;

		public ColorPicker() {
			InitializeComponent();
		}

		private void RectangleClicked(object sender, MouseButtonEventArgs e) {
			ColourPopup.IsOpen = !ColourPopup.IsOpen;
			e.Handled = true;
		}

		private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
			SelectedColorChanged?.Invoke(SelectedColor);
		}
	}
}