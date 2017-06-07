using System;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.Graphics.Drawable;
using Android.Views;
using Android.Widget;
using CrossBreed.Chat;
using CrossBreed.Entities;
using CrossBreed.ViewModels;
using ML.AppBase.Droid;
using MvvmCross.Binding.Droid.BindingContext;
using MvvmCross.Platform;
using MvvmCross.Plugins.DownloadCache;
using AlertDialog = Android.Support.V7.App.AlertDialog;

namespace CrossBreed.Droid {
	[Activity(Theme = "@style/ML.NoActionBar")]
	public class MainActivity : ToolbarNavigationActivity<MainViewModel> {
		public event Action<Toast> ToastSent;

		protected override void OnCreate(Bundle savedInstanceState) {
			base.OnCreate(savedInstanceState);
			if(Intent.HasExtra(Notifications.NotificationsSenderKey)) {
				ViewModel.ViewConversationCommand.Execute(Intent.GetStringExtra(Notifications.NotificationsSenderKey));
			}
			var header = this.BindingInflate(Resource.Layout.navigation_header, null);
			header.FindViewById(Resource.Id.status).Click +=
				delegate {
					new AlertDialog.Builder(this).SetPositiveButton(Strings.SetStatus_Title,
						(sender, args) => { ViewModel.ChangeStatusCommand.Execute(((AlertDialog) sender).FindViewById<TextView>(Resource.Id.input).Text); })
						.SetView(this.BindingInflate(Resource.Layout.dialog_change_status, null))
						.SetTitle(Strings.SetStatus_Title)
						.Show();
				};
			Mvx.GetSingleton<IMvxImageCache<Bitmap>>().RequestImage(ViewModel.Character.Image).ContinueWith(task => {
				var drawable = RoundedBitmapDrawableFactory.Create(Resources, task.Result);
				drawable.Circular = true;
				header.FindViewById<ImageView>(Resource.Id.icon).SetImageDrawable(drawable);
			});
			NavigationView.AddHeaderView(header);
		}

		protected override void OnStart() {
			base.OnStart();
			ViewModel.EventReceived += EventReceived;
			Notifications.RegisterUiHandler();
		}

		protected override void OnStop() {
			ViewModel.EventReceived -= EventReceived;
			Notifications.UnregisterUiHandler();
			base.OnStop();
		}

		private void EventReceived(Event e) {
			var msgEvent = e as MessageEvent;
			if(msgEvent != null) {
				ToastSent?.Invoke(new Toast(string.Format(Strings.Events_Message_TitleOne, msgEvent.Message.Sender.Name),
					Strings.Events_MessageCharacter, v => ViewModel.ViewConversationCommand.Execute(msgEvent.Message.Sender.Name)));
			}
			var connectionEvent = e as CharacterConnectionEvent;
			if(connectionEvent != null) {
				if(connectionEvent.Disconnected) {
					ToastSent?.Invoke(new Toast(string.Format(Strings.Events_CharacterOffline, connectionEvent.Character.Name)));
				} else {
					ToastSent?.Invoke(new Toast(string.Format(Strings.Events_CharacterOnline, connectionEvent.Character.Name),
						Strings.Events_MessageCharacter, v => ViewModel.ViewConversationCommand.Execute(connectionEvent.Character.Name)));
				}
			}
			var broadcastEvent = e as BroadcastEvent;
			if(broadcastEvent != null) {
				ToastSent?.Invoke(new Toast(Strings.Events_Broadcast, Strings.Events_ViewMessage, v => ViewModel.ViewHomeTabCommand.Execute(null)));
			}
			var errorEvent = e as ErrorEvent;
			if(errorEvent != null) {
				ToastSent?.Invoke(new Toast(string.Format(Strings.Events_Error, errorEvent.Message)));
			}
			var statusEvent = e as StatusEvent;
			if(statusEvent != null) {
				ToastSent?.Invoke(new Toast(string.Format(statusEvent.StatusMessage == "" ? Strings.Events_Status : Strings.Events_Status_Message,
					statusEvent.Character.Name, statusEvent.Status, statusEvent.StatusMessage),
					Strings.Events_MessageCharacter, v => ViewModel.ViewConversationCommand.Execute(statusEvent.Character.Name)));
			}
		}

		public class Toast {
			public string Text { get; }
			public string ActionText { get; }
			public Action<View> Action { get; }

			public Toast(string text, string actionText = null, Action<View> action = null) {
				Text = text;
				ActionText = actionText;
				Action = action;
			}
		}
	}
}