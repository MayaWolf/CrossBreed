using System;
using System.Windows.Media;
using ML.Settings;

namespace CrossBreed.Windows {
	public class ThemeSettings: BaseSettings {
		public static ThemeSettings Instance { get; }
		public Color BackgroundColor { get; set; } = Color.FromRgb(20, 20, 40);
		public event Action BackgroundColorChanged;

		public Color ForegroundColor { get; set; } = Colors.White;
		public event Action ForegroundColorChanged;

		public Color AccentColor { get; set; } = Colors.Purple;
		public event Action AccentColorChanged;

		public Color AccentForegroundColor { get; set; } = Colors.White;
		public event Action AccentForegroundColorChanged;

		[Properties(Min = 10, Max = 18)]
		public int FontSize { get; set; } = 15;
		public event Action FontSizeChanged;
	}
}