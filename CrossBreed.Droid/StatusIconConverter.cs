using System;
using System.Collections.Generic;
using System.Globalization;
using Android.App;
using Android.Graphics.Drawables;
using Android.Support.Graphics.Drawable;
using CrossBreed.Entities;
using MvvmCross.Platform.Converters;

namespace CrossBreed.Droid {
	public class StatusIconConverter:MvxValueConverter<StatusEnum, Drawable> {
		private static readonly Dictionary<StatusEnum, Drawable> cache = new Dictionary<StatusEnum, Drawable>(7);
		protected override Drawable Convert(StatusEnum value, Type targetType, object parameter, CultureInfo culture) {
			return GetDrawable(value);
		}

		public static Drawable GetDrawable(StatusEnum value) {
			if(!cache.ContainsKey(value)) {
				var drawable = VectorDrawableCompat.Create(Application.Context.Resources, GetResource(value), Application.Context.Theme);
				drawable.SetBounds(0, 0, drawable.IntrinsicWidth, drawable.IntrinsicHeight);
				cache[value] = drawable;
			}
			return cache[value];
		}

		private static int GetResource(StatusEnum value) {
			switch(value) {
				case StatusEnum.Offline:
					return Resource.Drawable.ic_status_offline;
				case StatusEnum.Online:
					return Resource.Drawable.ic_status_online;
				case StatusEnum.Away:
					return Resource.Drawable.ic_status_away;
				case StatusEnum.Idle:
					return Resource.Drawable.ic_status_idle;
				case StatusEnum.Looking:
					return Resource.Drawable.ic_status_looking;
				case StatusEnum.Busy:
					return Resource.Drawable.ic_status_busy;
				case StatusEnum.DND:
					return Resource.Drawable.ic_status_dnd;
				default:
					throw new ArgumentOutOfRangeException(nameof(value), value, null);
			}
		}
	}
}
