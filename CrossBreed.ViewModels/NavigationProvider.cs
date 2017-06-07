using System.Collections.Generic;
using ML.AppBase;

namespace CrossBreed.ViewModels {
	public class NavigationProvider: INavigationProvider {
		public IReadOnlyList<NavigationItem> Items { get; } = new List<NavigationItem> {
			new NavigationItem<ChatViewModel>(Strings.Tab_Chat, "ic_chat_black_24dp"),
			new NavigationItem<TabbedPeopleViewModel>(Strings.Tab_People, "ic_people_black_24dp"),
			new NavigationItem<ChannelsViewModel>(Strings.Tab_Channels, "ic_list_black_24dp"),
			new NavigationItem<FindPartnersViewModel>(Strings.Tab_FindPartners, "ic_search_black_24dp"),
			new NavigationItem<SettingsViewModel>(Strings.Tab_Settings, "ic_settings_black_24dp")
		};

		public NavigationItem Default => Items[0];
	}
}
