using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Graphics;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using ML.Droid.Base;

namespace CrossBreed.Droid {
	public class GroupedAdapter : RecyclerView.Adapter {
		private readonly Activity activity;
		private readonly IReadOnlyList<AdapterItem> items;

		public GroupedAdapter(Activity activity, IEnumerable<Item> items) {
			this.activity = activity;
			var types = new LinkedList<AdapterItem>();
			foreach(var group in items) {
				types.AddLast(new AdapterItem(0, group.Name, null));
				foreach(var item in Initialize(group.Items, 1)) types.AddLast(item);
			}
			this.items = types.ToList();
		}

		private static IEnumerable<AdapterItem> Initialize(IEnumerable<SubItem> items, int level) {
			foreach(var item in items) {
				yield return new AdapterItem(level, item.Name, item.Description);
				if(item.Items == null) continue;
				foreach(var subItem in Initialize(item.Items, level + 1)) yield return subItem;
			}
		}

		public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position) {
			holder.ItemView.FindViewById<TextView>(Android.Resource.Id.Text1).Text = items[position].Name;
			if(holder.ItemViewType != 0) holder.ItemView.FindViewById<TextView>(Android.Resource.Id.Text2).Text = items[position].Description;
		}

		public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) {
			var inflater = activity.LayoutInflater;
			View view;
			switch(viewType) {
				case 0:
					view = inflater.Inflate(Android.Resource.Layout.SimpleListItem1, parent, false);
					var text = view.FindViewById<TextView>(Android.Resource.Id.Text1);
					text.SetTextAppearance(activity, Android.Resource.Attribute.TextAppearanceLarge);
					var value = new TypedValue();
					activity.Theme.ResolveAttribute(Resource.Attribute.colorAccent, value, true);
					text.SetTextColor(new Color(value.Data));
					return new ViewHolder(view);
				default:
					view = inflater.Inflate(Android.Resource.Layout.SimpleListItem2, parent, false);
					view.FindViewById<TextView>(Android.Resource.Id.Text2).SetTextAppearance(activity, Android.Resource.Attribute.TextAppearanceMedium);
					view.SetPaddingRelative(MLHelpers.DpToPixelsInt(20), view.PaddingTop, view.PaddingEnd, view.PaddingBottom);
					return new ViewHolder(view);
			}
		}

		public override int GetItemViewType(int position) => items[position].Type;

		public override int ItemCount => items.Count;

		public class Item {
			public string Name { get; }
			public IEnumerable<SubItem> Items { get; }

			public Item(string name, IEnumerable<SubItem> items = null) {
				Name = name;
				Items = items;
			}
		}

		public class SubItem : Item {
			public string Description { get; }

			public SubItem(string name, string description, IEnumerable<SubItem> items = null) : base(name, items) {
				Description = description;
			}
		}

		private class AdapterItem {
			public string Name { get; }
			public string Description { get; set; }
			public int Type { get; set; }

			public AdapterItem(int type, string name, string description) {
				Type = type;
				Name = name;
				Description = description;
			}
		}

		private class ViewHolder : RecyclerView.ViewHolder {
			public ViewHolder(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {}
			public ViewHolder(View itemView) : base(itemView) {}
		}
	}
}