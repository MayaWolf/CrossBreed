using System.Windows;
using System.Windows.Media;

namespace CrossBreed.Windows.Views {
	public partial class IconButton {
		public static readonly DependencyProperty GeometryProperty = DependencyProperty.Register(nameof(Geometry), typeof(Geometry), typeof(IconButton));
		public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register("StrokeThickness", typeof(double), typeof(IconButton));

		public Geometry Geometry {
			get { return (Geometry) GetValue(GeometryProperty); }
			set { SetValue(GeometryProperty, value); }
		}

		public double StrokeThickness {
			get { return (double) GetValue(StrokeThicknessProperty); }
			set { SetValue(StrokeThicknessProperty, value); }
		}

		public IconButton() {
			InitializeComponent();
		}
	}
}