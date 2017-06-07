using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Views;
using CrossBreed.ViewModels;
using MvvmCross.Binding.Droid.BindingContext;
using MvvmCross.Binding.Droid.Views;
using MvvmCross.Droid.Shared.Attributes;
using MvvmCross.Droid.Support.V4;

namespace CrossBreed.Droid {
	[MvxFragment(typeof(MainViewModel), Resource.Id.content_frame, true)]
	[Register("crossbreed.droid.FindPartnersFragment")]
	public class FindPartnersFragment : MvxFragment<FindPartnersViewModel> {
		private Dialog progressDialog;
		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			base.OnCreateView(inflater, container, savedInstanceState);
			((MainActivity)Activity).ToastSent += toast => {
				var bar = Snackbar.Make(container, toast.Text, Snackbar.LengthLong);
				if(toast.Action != null) bar.SetAction(toast.ActionText, toast.Action);
				bar.Show();
			};
			ViewModel.PropertyChanged += (sender, args) => {
				switch(args.PropertyName) {
					case nameof(ViewModel.Pending):
						if(!ViewModel.Pending) return;
						progressDialog = new AlertDialog.Builder(Context).SetView(this.BindingInflate(Resource.Layout.dialog_progress, null)).Show();
						break;
					case nameof(ViewModel.Characters):
						progressDialog?.Dismiss();
						ShowResultDialog();
						break;
				}
			};
			return this.BindingInflate(Resource.Layout.fragment_find_partners, container, false);
		}

		private void ShowResultDialog() {
			var adapter = new MvxAdapter(Context, (IMvxAndroidBindingContext) BindingContext) {
				ItemsSource = ViewModel.Characters,
				ItemTemplateId = Resource.Layout.list_item_character_image
			};
			var dialog = new AlertDialog.Builder(Context).SetAdapter(adapter, (IDialogInterfaceOnClickListener) null).Show();
			dialog.ListView.ItemClick += (sender, args) => {
				ViewModel.ViewProfileCommand.Execute(ViewModel.Characters[args.Position]);
			};
		}
	}
}