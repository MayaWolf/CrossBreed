using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CodeKicker.BBCode.SyntaxTree;
using CrossBreed.Chat;
using CrossBreed.Entities;
using CrossBreed.Entities.ApiResponses;
using ML.AppBase;
using ML.Collections;
using MvvmCross.Core.Navigation;
using MvvmCross.Core.ViewModels;
using MvvmCross.Platform.UI;

namespace CrossBreed.ViewModels {
	public class CharacterViewModel : BaseViewModel<string> {
		private readonly ICharacterManager characterManager;
		private readonly IApiManager apiManager;
		private readonly CharacterListProvider characterListProvider;
		private ProfileViewModel profile;
		private IReadOnlyList<FriendStatusViewModel> friendStatuses;
		private readonly Locker friendSync = new Locker(), profileSync = new Locker();
		private static MappingListResponse mappings;
		private readonly ObservableList<CharacterList> characterLists = new ObservableList<CharacterList>(), availableLists = new ObservableList<CharacterList>();
		public bool AdminActionsAvailable => characterManager.OwnCharacter.IsChatOp;

		public Character Character { get; private set; }
		public MvxColor GenderColor => GetColor(Character);
		public string Image => GetAvatar(Character.Name);
		public string LocalizedGender => this[$"Gender_{Character.Gender}"];
		public string LocalizedStatus => this[$"Status_{Character.Status}"];
		public SyntaxTreeNode FormattedStatus => string.IsNullOrWhiteSpace(Character.StatusMessage) ? null : BbCodeParser.Parse(Character.StatusMessage);

		public IMvxCommand ShowProfileCommand { get; }
		public IMvxCommand MessageCommand { get; }
		public IMvxCommand KickCommand { get; }
		public IMvxCommand BanCommand { get; }

		public string ToggleBookmarkCommandName => Character.IsBookmarked ? Strings.Character_BookmarkRemove : Strings.Character_BookmarkAdd;
		public ICommand ToggleBookmarkCommand { get; }

		public string ToggleIgnoreCommandName => Character.IsIgnored ? Strings.Character_UnIgnore : Strings.Character_Ignore;
		public ICommand ToggleIgnoreCommand { get; }

		public IObservableList<CharacterList> CharacterLists => characterLists;
		public IObservableList<CharacterList> AvailableLists => availableLists;
		public ICommand AddToListCommand { get; }
		public ICommand RemoveFromListCommand { get; }

		public ProfileViewModel Profile {
			get {
				if(profile == null && !profileSync.IsLocked) GetProfile();
				return profile;
			}
		}

		public IReadOnlyList<FriendStatusViewModel> FriendStatuses {
			get {
				if(friendStatuses == null && !friendSync.IsLocked) GetFriendStatuses();
				return friendStatuses;
			}
		}

		public async Task<ProfileViewModel> GetProfile() {
			using(await profileSync.Lock()) {
				if(profile != null) return profile;
				if(mappings == null) mappings = (await apiManager.QueryApi("mapping-list.php")).ToObject<MappingListResponse>();
				var json = await apiManager.QueryApi("character-data.json", $"name={Character.Name}");
				profile = new ProfileViewModel(json.ToObject<ProfileResponse>(), mappings);
				RaisePropertyChanged(nameof(Profile));
				return profile;
			}
		}

		public async Task<IReadOnlyList<FriendStatusViewModel>> GetFriendStatuses() {
			using(await friendSync.Lock()) {
				if(friendStatuses != null) return friendStatuses;
				var result = await apiManager.QueryApi("character-list.php");
				var statuses = result.Value<IReadOnlyCollection<string>>("characters").ToDictionary(x => x, x => (FriendStatusViewModel) null);

				result = await apiManager.QueryApi("friend-list.php");
				foreach(var friend in result["friends"].ToObject<IEnumerable<FriendListItem>>().Where(x => x.dest == Character.Name)) {
					statuses[friend.source] = new FriendStatusViewModel(apiManager, friend.source, friend.dest, null, FriendStatusViewModel.StatusEnum.Friends);
				}

				result = await apiManager.QueryApi("request-list.php");
				foreach(var friend in result["requests"].ToObject<IEnumerable<FriendListItem>>().Where(x => x.source == Character.Name)) {
					statuses[friend.dest] = new FriendStatusViewModel(apiManager, friend.dest, friend.source, friend.id, FriendStatusViewModel.StatusEnum.Incoming);
				}

				result = await apiManager.QueryApi("request-pending.php");
				foreach(var friend in result["requests"].ToObject<IEnumerable<FriendListItem>>().Where(x => x.dest == Character.Name)) {
					statuses[friend.source] = new FriendStatusViewModel(apiManager, friend.source, friend.dest, friend.id, FriendStatusViewModel.StatusEnum.Outgoing);
				}

				friendStatuses = statuses.Select(x => x.Value ?? new FriendStatusViewModel(apiManager, x.Key, Character.Name, null, FriendStatusViewModel.StatusEnum.None)).ToList();
				RaisePropertyChanged(nameof(FriendStatuses));
				return friendStatuses;
			}
		}

		public CharacterViewModel(IChatManager chatManager, ICharacterManager characterManager, IApiManager apiManager, CharacterListProvider characterListProvider) {
			this.characterManager = characterManager;
			this.apiManager = apiManager;
			this.characterListProvider = characterListProvider;
			ToggleBookmarkCommand = new MvxCommand(() => apiManager.QueryApi($"bookmark-{(Character.IsBookmarked ? "remove" : "add")}.php?name={Character.Name}"));
			ToggleIgnoreCommand = new MvxCommand(() => characterManager.SetIgnored(Character, !Character.IsIgnored));
			ShowProfileCommand = new MvxCommand(() => Navigator.Navigate(this));
			MessageCommand = new MvxCommand(() => {
				Navigator.Navigate<ChatViewModel, ChatViewModel.InitArgs>(new ChatViewModel.InitArgs { Character = Character.Name });
			});
			
			KickCommand = new MvxCommand(() => {
				chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.KIK, new { character = Character.Name }));
			});
			BanCommand = new MvxCommand(() => {
				chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.ACB, new { character = Character.Name }));
			});
			AddToListCommand = new MvxCommand<CustomCharacterList>(list => characterListProvider.AddCharacter(list, Character.Name));
			RemoveFromListCommand = new MvxCommand<CustomCharacterList>(list => characterListProvider.RemoveCharacter(list, Character.Name));
			characterListProvider.CustomListsChanged += SetCharacterLists;
		}

		public static string GetAvatar(string name) => $"https://static.f-list.net/images/avatar/{name.ToLower()}.png";

		private void SetCharacterLists() {
			characterLists.Clear();
			availableLists.Clear();
			if(Character.IsFriend) characterLists.Add(characterListProvider.Friends);
			if(Character.IsBookmarked) characterLists.Add(characterListProvider.Bookmarks);
			if(Character.IsChatOp) characterLists.Add(characterListProvider.ChatOps);
			foreach(var list in characterListProvider.CustomLists) {
				if(list.Characters.Contains(Character.Name)) characterLists.Add(list);
				else availableLists.Add(list);
			}
		}

		internal CharacterViewModel(IChatManager chatManager, ICharacterManager characterManager, IApiManager apiManager, CharacterListProvider provider, Character character) :
			this(chatManager, characterManager, apiManager, provider) {
			SetCharacter(character);
		}

		public override async Task Initialize(string character) {
			SetCharacter(characterManager.GetCharacter(character));
		}

		private void SetCharacter(Character character) {
			Character = character;
			character.IsBookmarkedChanged += () => {
				RaisePropertyChanged(nameof(ToggleBookmarkCommandName));
				SetCharacterLists();
			};
			character.IsIgnoredChanged += () => RaisePropertyChanged(nameof(ToggleIgnoreCommandName));
			Character.IsChatOpChanged += SetCharacterLists;
			Character.IsFriendChanged += SetCharacterLists;
			SetCharacterLists();
		}

		public static MvxColor GetColor(Character character) {
			switch(character.Gender) {
				case GenderEnum.Male:
					return new MvxColor(0xff6699ff);
				case GenderEnum.Female:
					return new MvxColor(0xffff6699);
				case GenderEnum.Shemale:
					return new MvxColor(0xffcc66ff);
				case GenderEnum.Herm:
					return new MvxColor(0xff9b30ff);
				case GenderEnum.Maleherm:
					return new MvxColor(0xff007fff);
				case GenderEnum.Cuntboy:
					return new MvxColor(0xff00cc66);
				case GenderEnum.Transgender:
					return new MvxColor(0xffee8822);
				default:
					return new MvxColor(0xffffffbb);
			}
		}

		private class Locker {
			private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

			public async Task<IDisposable> Lock() {
				await semaphore.WaitAsync();
				return new Releaser(semaphore);
			}

			private class Releaser : IDisposable {
				private bool isDisposed;
				private readonly SemaphoreSlim semaphore;

				public Releaser(SemaphoreSlim semaphore) {
					this.semaphore = semaphore;
				}

				public void Dispose() {
					if(isDisposed) return;
					isDisposed = true;
					semaphore.Release();
				}
			}

			public bool IsLocked => semaphore.CurrentCount == 0;
		}
	}
}