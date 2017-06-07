using System;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Runtime;
using Android.Support.Graphics.Drawable;
using Android.Support.V4.Graphics.Drawable;
using Android.Util;
using Android.Views;
using Android.Widget;
using ML.Droid.Base;

namespace CrossBreed.Droid {
	public sealed class CollapsingView : LinearLayout {
		private readonly TextView titleTextView;
		private readonly ImageView iconView;
		private static Drawable expandDrawable, collapseDrawable;
		private bool isExpanded;
		public string Title {
			get { return titleTextView.Text; }
			set { titleTextView.Text = value; }
		}

		public CollapsingView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {}

		public CollapsingView(Context context, IAttributeSet attrs = null, int defStyleAttr = 0) : base(context, attrs, defStyleAttr) {
			if(expandDrawable == null) {
				expandDrawable = GetDrawable(Resource.Drawable.ic_expand_more_black_24dp);
				collapseDrawable = GetDrawable(Resource.Drawable.ic_expand_less_black_24dp);
			}
			var titleView = context.GetSystemService<LayoutInflater>(Context.LayoutInflaterService).Inflate(Resource.Layout.collapse_header, this);
			titleTextView = titleView.FindViewById<TextView>(Resource.Id.text);
			iconView = titleView.FindViewById<ImageView>(Resource.Id.icon);
			iconView.SetImageDrawable(expandDrawable);
			titleView.Click += delegate { Toggle(); };
			Orientation = Orientation.Vertical;
			ChildViewAdded += (sender, args) => args.Child.Visibility = isExpanded ? ViewStates.Visible : ViewStates.Gone;
			var padding = MLHelpers.DpToPixelsInt(5);
			SetPadding(padding, padding, padding, padding);
		}

		private Drawable GetDrawable(int resource) {
			var drawable = DrawableCompat.Wrap(VectorDrawableCompat.Create(Resources, resource, Context.Theme));
			var value = new TypedValue();
			Context.Theme.ResolveAttribute(Resource.Attribute.colorAccent, value, true);
			DrawableCompat.SetTint(drawable, value.Data);
			return drawable;
		}

		public void Collapse() {
			if(!isExpanded) return;
			for(var i = 1; i < ChildCount; ++i) GetChildAt(i).Visibility = ViewStates.Gone;
			iconView.SetImageDrawable(expandDrawable);
			isExpanded = false;
		}

		public void Expand() {
			if(isExpanded) return;
			for(var i = 1; i < ChildCount; ++i) GetChildAt(i).Visibility = ViewStates.Visible;
			iconView.SetImageDrawable(collapseDrawable);
			isExpanded = true;
		}

		public void Toggle() {
			if(isExpanded) Collapse();
			else Expand();
		}
	}
}