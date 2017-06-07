using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using CrossBreed.Chat;
using CrossBreed.Entities;
using CrossBreed.Entities.ClientMessages;
using ML.AppBase;
using MvvmCross.Core.ViewModels;
using MvvmCross.Platform;
using MvvmCross.Plugins.Messenger;

namespace CrossBreed.ViewModels {
	public class MainViewModel : NavigationViewModel {
		private readonly IChatManager chatManager;
		private readonly ICharacterManager characterManager;
		private readonly IMvxMessenger messenger;
		public MvxCommand<string> ViewConversationCommand { get; }
		public IReadOnlyList<StatusEnum> Statuses { get; } = new[] { StatusEnum.Online, StatusEnum.Looking, StatusEnum.Away, StatusEnum.Busy, StatusEnum.DND };
		public CharacterViewModel Character { get; }
		public ICommand ChangeStatusCommand { get; }
		public ICommand ViewHomeTabCommand { get; }

		public MainViewModel(IChatManager chatManager, ICharacterManager characterManager, INavigationProvider navigationProvider, IMvxMessenger messenger) : base(navigationProvider) {
			this.chatManager = chatManager;
			this.characterManager = characterManager;
			this.messenger = messenger;
			Character = Mvx.GetSingleton<CharacterViewModels>().GetCharacterViewModel(characterManager.OwnCharacter);
			ViewConversationCommand = new MvxCommand<string>(ShowCharacterTab);
			ChangeStatusCommand = new MvxCommand<Tuple<StatusEnum, string>>(tuple => chatManager.SetStatus(tuple.Item1, tuple.Item2));
			ViewHomeTabCommand = new MvxCommand(() => {
				if(messenger.HasSubscriptionsFor<ChatViewModel.ShowTabMessage>()) messenger.Publish(new ChatViewModel.ShowTabMessage(this));
				else Navigator.Navigate<ChatViewModel>();
			});
		}

		private void ShowCharacterTab(string character) {
			if(messenger.HasSubscriptionsFor<ChatViewModel.ShowTabMessage>()) {
				messenger.Publish(new ChatViewModel.ShowTabMessage(this) { Character = character });
			} else {
				Navigator.Navigate<ChatViewModel, ChatViewModel.InitArgs>(new ChatViewModel.InitArgs { Character = character });
			}
		}
	}
}