using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CrossBreed.Chat;
using CrossBreed.Entities;
using CrossBreed.ViewModels;
using MvvmCross.Platform;
using ML.DependencyProperty;

namespace CrossBreed.Windows.Views {
	public partial class MainView {
		private NotificationWindow notificationWindow;
		public ChatViewModel ChatViewModel { get; }
		public ListPeopleViewModel PeopleViewModel { get; }
		public EventsViewModel EventsViewModel { get; }

		[DependencyProperty]
		public ChannelMembersViewModel ChannelMembers { get; set; }

		[DependencyProperty]
		public bool IsHomeTabShown { get; set; }

		[DependencyProperty]
		public ListsViewModel CharacterLists { get; set; }

		public MainView() {
			var vmLoader = Mvx.GetSingleton<NavigationService>();
			Mvx.GetSingleton<IMessageManager>().CharacterMessageReceived += OnMessage;
			ChatViewModel = vmLoader.Load<ChatViewModel>();
			CharacterLists = vmLoader.Load<ListsViewModel>();
			EventsViewModel = vmLoader.Load<EventsViewModel>();
			PeopleViewModel = vmLoader.Load<ListPeopleViewModel>();
			IsHomeTabShown = ChatViewModel.SelectedTab == ChatViewModel.HomeTab;
			EventsViewModel.Events.CollectionChanged += (sender, args) => {
				if(args.Action != NotifyCollectionChangedAction.Add) return;
				foreach(EventViewModel evm in args.NewItems) OnEvent(evm);
			};
			var characterManager = Mvx.GetSingleton<ICharacterManager>();
			var channelManager = Mvx.GetSingleton<IChannelManager>();
			ChatViewModel.SelectedTabChanged += () => {
				IsHomeTabShown = ChatViewModel.SelectedTab == ChatViewModel.HomeTab;
				var channelTab = ChatViewModel.SelectedTab as ChannelConversationViewModel;
				ChannelMembers = channelTab != null ? new ChannelMembersViewModel(channelManager, characterManager, channelTab.Channel) : null;
				if(!IsHomeTabShown) MessagesView.ViewModel = (ConversationViewModel) ChatViewModel.SelectedTab;
			};
			InitializeComponent();
			DataContextChanged += OnViewModelSet;
		}

		private void OnViewModelSet(object sender, DependencyPropertyChangedEventArgs e) {
			App.Current.MainWindow.Title = "CrossBreed - " + ViewModel.Character.Character.Name;
		}

		private void OnEvent(EventViewModel evm) {
			var e = evm.Event;
			if(e is ErrorEvent || e is ChannelJoinEvent || e is ChannelLeaveEvent || e is SysEvent) return;
			Dispatcher.Invoke(() => ShowToast(new NotificationWindow(evm)));
			if(UserSettings.Instance.Notifications.Sounds) {
				if(e is MentionEvent) PlaySound("message");
				else if(e is NoteEvent) PlaySound("note");
			}
		}

		private void OnMessage(Character character, Message message) {
			var isCurrentTabMessage = character == (ChatViewModel.SelectedTab as CharacterConversationViewModel)?.Character.Character;
			if(!isCurrentTabMessage) Dispatcher.Invoke(() => ShowToast(new NotificationWindow(message)));
			if(!isCurrentTabMessage || UserSettings.Instance.Notifications.AlwaysPlaySound) PlaySound("message");
		}

		private void ShowToast(NotificationWindow window) {
			var notificationSettings = UserSettings.Instance.Notifications;
			if(notificationSettings.Toasts) {
				notificationWindow?.Close();
				notificationWindow = window;
				notificationWindow.Show();
			}
		}

		private void PlaySound(string name) {
			var player = new MediaPlayer();
			var file = Directory.EnumerateFiles("Sounds").FirstOrDefault(x => Path.GetFileNameWithoutExtension(x) == name);
			if(file == null) return;
			player.Open(new Uri(Path.GetFullPath(file)));
			player.Play();
		}

		private void StatusButtonClicked(object sender, RoutedEventArgs e) {
			StatusPopup.IsOpen = true;
		}
	}
}