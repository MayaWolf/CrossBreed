using System;
using System.Globalization;
using Android.App;
using Android.Graphics.Drawables;
using Android.Support.Graphics.Drawable;
using CrossBreed.Entities;
using MvvmCross.Platform.Converters;

namespace CrossBreed.Droid {
	public class TypingConverter:MvxValueConverter<TypingStatusEnum, Drawable> {
		protected override Drawable Convert(TypingStatusEnum value, Type targetType, object parameter, CultureInfo culture) {
			var context = Application.Context;
			switch(value) {
				case TypingStatusEnum.Clear:
					return null;
				case TypingStatusEnum.Paused:
					return VectorDrawableCompat.Create(context.Resources, Resource.Drawable.ic_typing_paused, context.Theme);
				case TypingStatusEnum.Typing:
					return VectorDrawableCompat.Create(context.Resources, Resource.Drawable.ic_typing, context.Theme);
				default:
					throw new ArgumentOutOfRangeException(nameof(value), value, null);
			}
		}
	}
}