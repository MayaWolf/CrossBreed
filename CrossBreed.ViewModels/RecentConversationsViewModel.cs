using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using CrossBreed.Chat;
using ML.AppBase;
using MvvmCross.Core.ViewModels;

namespace CrossBreed.ViewModels {
	public class RecentConversationsViewModel : BaseViewModel {
		public IReadOnlyCollection<Tuple<string, string>> RecentChannels { get; set; }
		public IReadOnlyCollection<CharacterViewModel> RecentCharacters { get; set; }
		public ICommand JoinChannelCommand { get; }

		public RecentConversationsViewModel(IChannelManager channelManager, CharacterViewModels cache) {
			var settings = CharacterSettings.Instance;
			RecentChannels = settings.RecentChannels;
			settings.RecentChannelsChanged += () => RecentChannels = settings.RecentChannels;
			RecentCharacters = settings.RecentCharacters.Select(cache.GetCharacterViewModel).ToList();
			settings.RecentCharactersChanged += () => RecentCharacters = settings.RecentCharacters.Select(cache.GetCharacterViewModel).ToList();
			JoinChannelCommand = new MvxCommand<string>(channelManager.JoinChannel);
		}
	}
}