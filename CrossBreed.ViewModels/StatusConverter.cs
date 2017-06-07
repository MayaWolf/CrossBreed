using System;
using System.Globalization;
using CrossBreed.Chat;
using CrossBreed.Entities;
using MvvmCross.Platform.Converters;

namespace CrossBreed.ViewModels {
	public class StatusConverter:MvxValueConverter<StatusEnum, string> {
		protected override string Convert(StatusEnum value, Type targetType, object parameter, CultureInfo culture) {
			return Strings.ResourceManager.GetString("Status_" + value);
		}
	}
}
