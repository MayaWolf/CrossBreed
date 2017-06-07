using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CrossBreed.Chat;
using CrossBreed.ViewModels;
using MvvmCross.Core.Navigation;
using MvvmCross.Platform;

namespace CrossBreed.Windows.Views {
	public partial class ConversationView {
		private static readonly CharacterViewModels characterCache = Mvx.GetSingleton<CharacterViewModels>();
		private Func<string, UserView> userViewFactory;

		public ConversationView() {
			InitializeComponent();
			if(UserSettings.Instance.General.MessagesFromBottom) TextBlock.VerticalAlignment = VerticalAlignment.Bottom;
			DataContextChanged += ConversationChanged;
		}

		private void ConversationChanged(object sender, DependencyPropertyChangedEventArgs args) {
			if(args.OldValue is ConversationViewModel oldValue) oldValue.Messages.CollectionChanged -= MessageCollectionChanged;
			TextBlock.Document = new FlowDocument();

			if(ViewModel == null) return;
			if(ViewModel is ChannelConversationViewModel ccvm) {
				var channel = ccvm.Channel;
				var characterManager = Mvx.GetSingleton<ICharacterManager>();
				var channelManager = Mvx.GetSingleton<IChannelManager>();
				userViewFactory = name => {
					var character = characterManager.GetCharacter(name);
					if(!channel.Members.ContainsKey(character)) return new UserView { Character = characterCache.GetCharacterViewModel(name) };
					var member = channel.Members[character];
					return new UserView { ChannelMember = new ChannelMemberViewModel(characterManager, channelManager, channel, member) };
				};
			} else userViewFactory = name => new UserView { Character = characterCache.GetCharacterViewModel(name) };
			foreach(var message in ViewModel.Messages.ToListAndRegister(MessageCollectionChanged)) AddMessage(message);
		}

		private void MessageCollectionChanged(object sender, NotifyCollectionChangedEventArgs args) {
			switch(args.Action) {
				case NotifyCollectionChangedAction.Add:
					foreach(var message in args.NewItems.Cast<MessageViewModel>()) AddMessage(message);
					break;
				case NotifyCollectionChangedAction.Reset:
					TextBlock.Document = new FlowDocument();
					foreach(var message in ViewModel.Messages.ToList()) AddMessage(message);
					break;
			}
		}

		private void AddMessage(MessageViewModel message) {
			TextBlock.Document.Blocks.Add(new Paragraph(BBCode.ToInlines(message.Formatted, tag => {
				if(tag.Tag.Name != "user") return null;
				return new InlineUIContainer(userViewFactory(tag.ToText()));
			})) { Margin = new Thickness(0, 0, 0, App.Current.Theme.FontSize / 3) }); //TODO backlog, new
		}

		private void ShowSettings(object sender, RoutedEventArgs e) {
			if(ViewModel is ChannelConversationViewModel channel) SettingsPopup.DataContext = new ChannelConversationSettingsViewModel(channel.Channel);
			else SettingsPopup.DataContext = new PrivateConversationSettingsViewModel(((CharacterConversationViewModel) ViewModel).Character.Character);
			SettingsPopup.IsOpen = true;
		}

		private void EventSetter_OnHandler(object sender, RoutedEventArgs e) {
			((FrameworkElement) VisualTreeHelper.GetParent((DependencyObject) sender)).HorizontalAlignment = HorizontalAlignment.Stretch;
		}

		private void ShowLogs(object sender, RoutedEventArgs e) {
			var vm = Mvx.IocConstruct<LogsViewModel>();
			if(ViewModel is ChannelConversationViewModel channel) vm.SetChannel(channel.Channel);
			else vm.SetCharacter(((CharacterConversationViewModel) ViewModel).Character.Character);
			Mvx.GetSingleton<IMvxNavigationService>().Navigate(vm);
		}
	}

	public class NotificationSettingsView : UserControl {
		public static readonly DependencyProperty NotifySettingsProperty = DependencyProperty.Register(nameof(NotifySettings), typeof(NotifySettings), typeof(NotificationSettingsView));
		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(NotificationSettingsView));

		public NotifySettings NotifySettings {
			get => (NotifySettings) GetValue(NotifySettingsProperty);
			set => SetValue(NotifySettingsProperty, value);
		}

		public string Title {
			get => (string) GetValue(TitleProperty);
			set => SetValue(TitleProperty, value);
		}
	}
}