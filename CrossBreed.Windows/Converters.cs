using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using MvvmCross.Platform.ExtensionMethods;
using MvvmCross.Platform.UI;
using MvvmCross.Platform.Wpf.Converters;
using MvvmCross.Plugins.Color;

namespace CrossBreed.Windows {
	public class VisibilityConverter : IValueConverter {
		private static readonly BoolConverter BoolConverter = new BoolConverter();

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return (bool) BoolConverter.Convert(value, targetType, parameter, culture) ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}

	public class MvxColorConverter : MvxNativeValueConverter<MvxNativeColorValueConverter> {
		public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return ((SolidColorBrush) base.Convert(value, targetType, parameter, culture)).Color;
		}

		public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			var color = (Color) value;
			return new MvxColor(color.R, color.G, color.B, color.A);
		}
	}

	public class MvxColorBrushConverter : MvxNativeValueConverter<MvxNativeColorValueConverter> {}

	public class BoolConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var b = value.ConvertToBooleanCore();
			if(b && value is ICollection) b = ((ICollection) value).Count > 0;
			return "Negate".Equals(parameter) ^ b;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}

	public class TypeConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return value?.GetType();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}

	public class ColorBrushConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return new SolidColorBrush((Color) value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			return ((SolidColorBrush) value).Color;
		}
	}

	public class StatusConverter : MvxNativeValueConverter<ViewModels.StatusConverter> {}
}