using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Views;
using CrossBreed.Chat;
using CrossBreed.ViewModels;
using ML.Settings;
using MvvmCross.Droid.Shared.Attributes;

namespace CrossBreed.Droid {
	[MvxFragment(typeof(MainViewModel), Resource.Id.content_frame, true)]
	[Register("crossbreed.droid.SettingsFragment")]
	public class SettingsFragment : ML.AppBase.Droid.SettingsFragment {
		protected override ISettings Settings => UserSettings.Instance;

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			((MainActivity)Activity).ToastSent += toast => {
				var bar = Snackbar.Make(container, toast.Text, Snackbar.LengthLong);
				if(toast.Action != null) bar.SetAction(toast.ActionText, toast.Action);
				bar.Show();
			};
			return base.OnCreateView(inflater, container, savedInstanceState);
		}
	}
}