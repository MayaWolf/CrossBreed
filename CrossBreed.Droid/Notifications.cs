using System.Collections.Specialized;
using System.Linq;
using Android.App;
using Android.Graphics;
using Android.Support.V7.App;
using CrossBreed.Chat;
using CrossBreed.Entities;
using ML.Droid.Base;
using MvvmCross.Platform;
using MvvmCross.Plugins.DownloadCache;

namespace CrossBreed.Droid {
	public static class Notifications {
		private static int uiHandlers;
		private static int notificationCount;
		private static readonly int notificationId = Intents.GetPendingIntentRequestCode();
		public const string NotificationsSenderKey = "sender";
		private static readonly UserSettings.NotificationsSettings settings = UserSettings.Instance.Notifications;

		public static void Initialize() {
			Mvx.GetSingleton<INotificationManager>().NewEvent += HandleEvent;
		}

		private static async void HandleEvent(Event e) {
			var msgEvent = e as MessageEvent;
			if(uiHandlers != 0 || msgEvent == null || !settings.NotifyPrivate) return;
			var intent = Intents.Create<MainActivity>();
			if(notificationCount == 0) intent.PutExtra(NotificationsSenderKey, msgEvent.Message.Sender.Name);
			var builder = new NotificationCompat.Builder(Application.Context).SetAutoCancel(true)
				.SetContentTitle(notificationCount > 0 ? Strings.Events_Message_TitleMany : string.Format(Strings.Events_Message_TitleOne, msgEvent.Message.Sender.Name))
				.SetContentText(notificationCount > 0 ? Strings.Events_Message_TextMany : msgEvent.Message.Text)
				.SetSmallIcon(Resource.Drawable.icon)
				.SetContentIntent(PendingIntent.GetActivity(Application.Context, notificationId, intent, PendingIntentFlags.UpdateCurrent))
				.SetLargeIcon(await Mvx.GetSingleton<IMvxImageCache<Bitmap>>().RequestImage(Helpers.GetAvatar(msgEvent.Message.Sender.Name)));
			if(settings.Vibrate) builder.SetVibrate(new long[] { 500, 500 });
			Android.App.NotificationManager.FromContext(Application.Context).Notify(notificationId, builder.Build());
			++notificationCount;
		}

		public static void RegisterUiHandler() {
			++uiHandlers;
			notificationCount = 0;
		}

		public static void UnregisterUiHandler() => --uiHandlers;
	}
}