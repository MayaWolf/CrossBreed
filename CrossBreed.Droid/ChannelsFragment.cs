using System.Linq;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Com.Androidadvance.Topsnackbar;
using CrossBreed.Chat;
using CrossBreed.ViewModels;
using ML.AppBase.Droid;
using MvvmCross.Binding.Droid.BindingContext;
using MvvmCross.Droid.Shared.Attributes;
using MvvmCross.Droid.Support.V4;

namespace CrossBreed.Droid {
	[MvxFragment(typeof(MainViewModel), Resource.Id.content_frame, true)]
	[Register("crossbreed.droid.ChannelsFragment")]
	public class ChannelsFragment : MvxFragment<ChannelsViewModel> {
		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			base.OnCreateView(inflater, container, savedInstanceState);
			HasOptionsMenu = true;
			var view = this.BindingInflate(Resource.Layout.fragment_channels, container, false);
			((MainActivity)Activity).ToastSent += toast => {
				var bar = TSnackbar.Make(view.FindViewById(Android.Resource.Id.Content), toast.Text, TSnackbar.LengthLong);
				if(toast.Action != null) bar.SetAction(toast.ActionText, toast.Action);
				bar.Show();
			};
			var tabLayout = view.FindViewById<TabLayout>(Resource.Id.tabs);
			var tabs = ViewModel.Tabs.ToDictionary(x => tabLayout.NewTab().SetText(x.Name));
			foreach(var tab in tabs.Keys) tabLayout.AddTab(tab);
			tabLayout.TabSelected += (sender, args) => ViewModel.TabSelectedCommand.Execute(tabs[args.Tab]);
			return view;
		}

		public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater) {
			base.OnCreateOptionsMenu(menu, inflater);
			inflater.LocalizedInflate(Resource.Menu.channels, menu);
		}

		public override bool OnOptionsItemSelected(IMenuItem item) {
			switch(item.ItemId) {
				case Resource.Id.refresh:
					ViewModel.RefreshCommand.Execute(null);
					break;
				case Resource.Id.add:
					new AlertDialog.Builder(Context).SetPositiveButton(Strings.SetStatus_Title, (sender, args) => {
						ViewModel.CreateCommand.Execute(((AlertDialog) sender).FindViewById<TextView>(Resource.Id.input).Text);
					}).SetView(this.BindingInflate(Resource.Layout.dialog_create_channel, null)).SetTitle(Strings.SetStatus_Title).Show();
					break;
			}
			return base.OnOptionsItemSelected(item);
		}
	}
}