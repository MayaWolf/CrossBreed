using Android.App;
using Android.OS;
using CrossBreed.Chat;
using CrossBreed.ViewModels;
using ML.AppBase.Droid;
using MvvmCross.Binding.Droid.BindingContext;

namespace CrossBreed.Droid {
	[Activity(Label = nameof(CrossBreed), Icon = "@drawable/icon", MainLauncher = true)]
	public class LoginActivity : BaseActivity<LoginViewModel> {
		private Dialog progressDialog;

		protected override void OnCreate(Bundle bundle) {
			base.OnCreate(bundle);
			Websockets.Droid.WebsocketConnection.Link();
			ViewModel.ConnectingChanged += () => {
				if(ViewModel.Connecting) {
					progressDialog = new Dialog(this);
					progressDialog.SetTitle(Strings.Login_Connecting);
					progressDialog.SetContentView(this.BindingInflate(Resource.Layout.dialog_progress, null));
					progressDialog.SetCancelable(false);
					progressDialog.Show();
				}
			};
		}

		protected override void OnDestroy() {
			progressDialog?.Dismiss();
			base.OnDestroy();
		}

		protected override void OnViewModelSet() {
			base.OnViewModelSet();
			SetContentView(Resource.Layout.activity_login);
		}
	}
}