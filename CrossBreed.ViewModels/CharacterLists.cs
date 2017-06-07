using System;
using System.Collections.Generic;
using System.Linq;
using ML.AppBase;

namespace CrossBreed.ViewModels {
	public class CharacterList {
		public string Name { get; set; }
		public int SortingOrder { get; set; }
		public uint TextColor { get; set; }
		public uint UnderlineColor { get; set; }
		public bool HideAds { get; set; }
		public bool HideInSearch { get; set; }
	}

	public class CustomCharacterList : CharacterList {
		public Guid Id { get; }
		public ISet<string> Characters { get; set; } = new HashSet<string>();

		public CustomCharacterList(Guid id) {
			Id = id;
		}
	}

	public class CharacterListProvider {
		private readonly ICharacterListStorage storage;
		private readonly IDialogProvider dialogs;
		private IReadOnlyCollection<CustomCharacterList> customLists;
		private IReadOnlyDictionary<string, CharacterList> defaultLists;

		public CharacterListProvider(ICharacterListStorage storage, IDialogProvider dialogs) {
			this.storage = storage;
			this.dialogs = dialogs;
		}

		public IReadOnlyDictionary<string, CharacterList> DefaultLists {
			get => defaultLists ?? ReloadDefaultLists();
			set => storage.DefaultLists = (defaultLists = value).Values.ToList();
		}

		public IReadOnlyCollection<CustomCharacterList> CustomLists => customLists ?? ReloadCustomLists();

		public event Action CustomListsChanged;

		public CharacterList Friends => DefaultLists[nameof(Friends)];
		public CharacterList Bookmarks => DefaultLists[nameof(Bookmarks)];
		public CharacterList ChatOps => DefaultLists[nameof(ChatOps)];
		public CharacterList ChannelOps => DefaultLists[nameof(ChannelOps)];

		public void SaveCustomLists(IReadOnlyCollection<CustomCharacterList> value) {
			var oldLists = storage.CustomLists;
			if(oldLists != null) {
				foreach(var list in value) {
					var oldList = oldLists.FirstOrDefault(x => x.Id == list.Id);
					if(oldList != null) list.Characters = oldList.Characters;
				}
			}
			storage.CustomLists = customLists = value;
			CustomListsChanged?.Invoke();
		}

		public IReadOnlyCollection<CustomCharacterList> ReloadCustomLists() {
			customLists = storage.CustomLists;
			if(customLists == null) {
				storage.CustomLists = customLists = new[] {
					new CustomCharacterList(Guid.NewGuid()) { Name = "Interesting", SortingOrder = 6, UnderlineColor = 0xFF800080 },
					new CustomCharacterList(Guid.NewGuid()) { Name = "Not interesting", SortingOrder = 20, HideAds = true, TextColor = 0xFF999999 }
				};
			}
			CustomListsChanged?.Invoke();
			return customLists;
		}

		public IReadOnlyDictionary<string, CharacterList> ReloadDefaultLists() {
			var lists = storage.DefaultLists;
			if(lists == null) {
				storage.DefaultLists = lists = new[] {
					new CharacterList { SortingOrder = 3, UnderlineColor = 0xFF00CC00, Name = nameof(Friends) },
					new CharacterList { SortingOrder = 5, UnderlineColor = 0xFF009900, Name = nameof(Bookmarks) },
					new CharacterList { SortingOrder = 8, UnderlineColor = 0xFFCC0000, Name = nameof(ChatOps) },
					new CharacterList { SortingOrder = 9, UnderlineColor = 0xFF990000, Name = nameof(ChannelOps) }
				};
			}
			return defaultLists = lists.ToDictionary(x => x.Name);
		}

		public async void AddCharacter(CustomCharacterList list, string character) {
			var lists = storage.CustomLists;
			var oldList = lists.FirstOrDefault(x => x.Id == list.Id);
			if(oldList == null) {
				if(await dialogs.Confirm(Strings.CharacterLists_Conflict, Strings.CharacterLists_Conflict_Overwrite, Strings.CharacterLists_Conflict_Reload)) {
					var newLists = lists.ToList();
					newLists.Add(list);
					list.Characters.Add(character);
					lists = newLists;
					storage.CustomLists = customLists = lists;
				} else {
					customLists = lists;
				}
			} else {
				oldList.Characters.Add(character);
				storage.CustomLists = customLists = lists;
			}
			CustomListsChanged?.Invoke();
		}

		public void RemoveCharacter(CustomCharacterList list, string character) {
			var lists = storage.CustomLists;
			var oldList = lists.FirstOrDefault(x => x.Id == list.Id);
			if(oldList == null) {
				customLists = lists;
			} else {
				oldList.Characters.Remove(character);
				storage.CustomLists = customLists = lists;
			}
			CustomListsChanged?.Invoke();
		}
	}

	public interface ICharacterListStorage {
		IReadOnlyCollection<CharacterList> DefaultLists { get; set; }
		IReadOnlyCollection<CustomCharacterList> CustomLists { get; set; }
	}
}