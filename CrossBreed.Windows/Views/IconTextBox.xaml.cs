using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CrossBreed.Windows.Views {
	public partial class IconTextBox {
		public ImageSource IconSource {
			get { return (ImageSource) GetValue(IconSourceProperty); }
			set { SetValue(IconSourceProperty, value); }
		}

		public static readonly DependencyProperty IconSourceProperty = DependencyProperty.Register(nameof(IconSource), typeof(ImageSource), typeof(IconTextBox));

		public Path Path {
			get { return (Path) GetValue(PathProperty); }
			set { SetValue(PathProperty, value); }
		}

		public static readonly DependencyProperty PathProperty = DependencyProperty.Register(nameof(Path), typeof(Path), typeof(IconTextBox));


		public IconTextBox() {
			InitializeComponent();
		}
	}
}