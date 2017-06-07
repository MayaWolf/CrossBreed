using System;
using System.Collections.Generic;
using CrossBreed.Chat;
using ML.Settings;

namespace CrossBreed.ViewModels {
	public class ClientSettings : BaseSettings {
		public static ClientSettings Instance { get; }

		public string UserName { get; set; }
		public string Password { get; set; }
		public string Host { get; set; } = "wss://chat.f-list.net:9799";
		public bool SaveLogin { get; set; }

		/*public CharacterListsSettings CharacterLists { get; }

		public class CharacterListsSettings : BaseSettings {
			public IReadOnlyCollection<CharacterList> CustomLists { get; set; } = new[] {
				new CharacterList { Name = "Interesting", SortingOrder = 6, UnderlineColor = 0xFF800080 },
				new CharacterList { Name = "Not interesting", SortingOrder = 20, HideAds = true, TextColor = 0xFF999999 }
			};

			public event Action CustomListsChanged;

			public CharacterList Friends { get; set; } = new CharacterList { SortingOrder = 3, UnderlineColor = 0xFF00CC00 };
			public CharacterList Bookmarks { get; set; } = new CharacterList { SortingOrder = 5, UnderlineColor = 0xFF009900 };
			public CharacterList ChatOps { get; set; } = new CharacterList { SortingOrder = 8, UnderlineColor = 0xFFCC0000 };
			public CharacterList ChannelOps { get; set; } = new CharacterList { SortingOrder = 9, UnderlineColor = 0xFF990000 };
		}*/
	}

	public class CharacterSettings: BaseSettings {
		public static CharacterSettings Instance { get; }

		public IReadOnlyCollection<string> PinnedCharacters { get; set; } = new string[] { };

		public IReadOnlyCollection<ChannelConversationSettings> ChannelSettings { get; set; } = new ChannelConversationSettings[] { };
		public IReadOnlyCollection<PrivateConversationSettings> PrivateSettings { get; set; } = new PrivateConversationSettings[] { };

		public IReadOnlyCollection<Tuple<string, string>> RecentChannels { get; set; } = new Tuple<string, string>[0];
		public event Action RecentChannelsChanged;
		public IReadOnlyCollection<string> RecentCharacters { get; set; } = new string[0];
		public event Action RecentCharactersChanged;
	}

	public class UserSettings : BaseSettings {
		public static UserSettings Instance { get; }

		public GeneralSettings General { get; }

		public class GeneralSettings : BaseSettings {
			public bool UseProfileViewer { get; set; } = true;
			public IReadOnlyCollection<string> DisallowedBbCode { get; set; } = new string[] { };
			public event Action DisallowedBbCodeChanged;
			public bool MessagesFromBottom { get; set; }
			public bool SortCharactersFirst { get; set; }
			public bool AutoLink { get; set; } = true;
			public bool CloseToTray { get; set; }
			public bool AlwaysIncognito { get; set; }
		}

		public AppSettings.LoggingSettings Logging { get; }

		public NotificationsSettings Notifications { get; }

		public class NotificationsSettings : BaseSettings {
			public bool Toasts { get; set; } = true;
			public bool Vibrate { get; set; }
			public bool Sounds { get; set; } = true;
			public bool AlwaysPlaySound { get; set; }

			public bool NotifyPrivate { get; set; } = true;
			public bool NotifyConnection { get; set; } = true;
			public bool NotifyStatus { get; set; } = true;
			public bool NotifyHighlights { get; set; } = true;

			public IReadOnlyCollection<string> HighlightWords { get; set; } = new string[0];
		}
	}
}