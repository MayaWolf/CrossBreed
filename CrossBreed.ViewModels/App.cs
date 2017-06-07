using System;
using System.Collections.Generic;
using CrossBreed.Chat;
using CrossBreed.Entities;
using ML.AppBase;
using ML.Settings;
using MvvmCross.Core.ViewModels;
using MvvmCross.Platform;

namespace CrossBreed.ViewModels {
	public class App : MvxApplication, IMvxViewModelLocatorCollection {
		public App() {
			Mvx.ConstructAndRegisterSingleton<IApiManager, ApiManager>();
			Mvx.ConstructAndRegisterSingleton<IChatManager, ChatManager>();
			Mvx.ConstructAndRegisterSingleton<ICharacterManager, CharacterManager>();
			Mvx.ConstructAndRegisterSingleton<IChannelManager, ChannelManager>();
			Mvx.ConstructAndRegisterSingleton<IMessageManager, MessageManager>();
			Mvx.ConstructAndRegisterSingleton<IEventManager, EventManager>();
			Mvx.ConstructAndRegisterSingleton<ILogManager, LogManager>();
			Mvx.ConstructAndRegisterSingleton<INavigationProvider, NavigationProvider>();
			Mvx.ConstructAndRegisterSingleton<CharacterListProvider, CharacterListProvider>();
			Mvx.LazyConstructAndRegisterSingleton<ISettings, UserSettings>();
			Mvx.LazyConstructAndRegisterSingleton<CharacterViewModels, CharacterViewModels>();
			RegisterAppStart<LoginViewModel>();
		}

		public new IMvxViewModelLocator FindViewModelLocator(MvxViewModelRequest request) {
			if(request is MvxViewModelInstanceRequest instance) return new InstanceViewModelLoader(instance.ViewModelInstance);
			return base.FindViewModelLocator(request);
		}

		private class InstanceViewModelLoader : MvxDefaultViewModelLocator {
			private readonly IMvxViewModel viewModel;

			public InstanceViewModelLoader(IMvxViewModel viewModel) {
				this.viewModel = viewModel;
			}

			public override IMvxViewModel Load(Type viewModelType, IMvxBundle parameterValues, IMvxBundle savedState) {
				RunViewModelLifecycle(viewModel, parameterValues, savedState);
				return viewModel;
			}
		}
	}

	public class CharacterViewModels {
		private readonly IDictionary<Character, CharacterViewModel> viewModels = new Dictionary<Character, CharacterViewModel>();
		private readonly IChatManager chatManager;
		private readonly ICharacterManager characterManager;
		private readonly IApiManager apiManager;
		private readonly CharacterListProvider characterListProvider;

		public CharacterViewModels(IChatManager chatManager, ICharacterManager characterManager, IApiManager apiManager, CharacterListProvider characterListProvider) {
			this.chatManager = chatManager;
			this.characterManager = characterManager;
			this.apiManager = apiManager;
			this.characterListProvider = characterListProvider;
		}

		public CharacterViewModel GetCharacterViewModel(Character character) {
			if(!viewModels.ContainsKey(character)) viewModels[character] = new CharacterViewModel(chatManager, characterManager, apiManager, characterListProvider, character);
			return viewModels[character];
		}

		public CharacterViewModel GetCharacterViewModel(string character) => GetCharacterViewModel(characterManager.GetCharacter(character));
	}
}