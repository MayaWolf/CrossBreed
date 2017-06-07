using System;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using CrossBreed.Entities;
using CrossBreed.ViewModels;

namespace CrossBreed.Windows.Views {
	public partial class NotificationWindow {
		private Timer fadeOutTimer;

		public NotificationWindow(EventViewModel evm) {
			InitializeComponent();
			switch(evm.Event) {
				case LoginEvent loginEvent:
					Title = loginEvent.Character.Name;
					Image.Source = new BitmapImage(new Uri(CharacterViewModel.GetAvatar(loginEvent.Character.Name)));
					break;
				case LogoutEvent logoutEvent:
					Title = logoutEvent.Character.Name;
					Image.Source = new BitmapImage(new Uri(CharacterViewModel.GetAvatar(logoutEvent.Character.Name)));
					break;
				case BroadcastEvent broadcastEvent:
					Title = Strings.Events_Broadcast_Notification;
					Image.Source = new BitmapImage(new Uri(CharacterViewModel.GetAvatar(broadcastEvent.Character.Name)));
					break;
				case StatusEvent statusEvent:
					Title = statusEvent.Character.Name;
					Image.Source = new BitmapImage(new Uri(CharacterViewModel.GetAvatar(statusEvent.Character.Name)));
					break;
				case NoteEvent noteEvent:
					Title = noteEvent.Character.Name;
					Image.Source = new BitmapImage(new Uri(CharacterViewModel.GetAvatar(noteEvent.Character.Name)));
					break;
				case MentionEvent highlightEvent:
					Title = string.Format(Strings.Events_Highlight_Notification, highlightEvent.Channel.Name);
					Image.Source = new BitmapImage(new Uri(CharacterViewModel.GetAvatar(highlightEvent.Message.Sender.Name)));
					break;
				default:
					return;
			}
			MessageView.BbCode = evm.Formatted;

			Loaded += OnLoaded;
		}

		public NotificationWindow(Message message) {
			InitializeComponent();
			Title = string.Format(Strings.Events_Message_TitleOne, message.Sender.Name);
			MessageView.BbCode = BbCodeParser.Parse(message.Text);
			Image.Source = new BitmapImage(new Uri(CharacterViewModel.GetAvatar(message.Sender.Name)));
			Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs args) {
			var workArea = SystemParameters.WorkArea;
			Left = workArea.Right - Width - BorderThickness.Right;
			Top = workArea.Bottom - Height - BorderThickness.Bottom;
			BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5)));
			fadeOutTimer = new Timer(5000) { AutoReset = false };
			fadeOutTimer.Elapsed += (s, e) => Dispatcher.Invoke(FadeOut);
			fadeOutTimer.Start();
		}

		private void FadeOut() {
			var fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.5));
			fadeOutAnimation.Completed += (s, e) => Close();
			BeginAnimation(OpacityProperty, fadeOutAnimation);
		}

		protected override void OnMouseEnter(MouseEventArgs e) {
			fadeOutTimer.Stop();
			BeginAnimation(OpacityProperty, null);
			Opacity = 1;
		}

		protected override void OnMouseLeave(MouseEventArgs e) {
			base.OnMouseLeave(e);
			fadeOutTimer.Start();
		}

		private void CloseButtonClicked(object sender, RoutedEventArgs routedEventArgs) {
			Close();
		}
	}
}