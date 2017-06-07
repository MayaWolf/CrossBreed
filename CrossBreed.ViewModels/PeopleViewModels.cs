using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using CrossBreed.Chat;
using CrossBreed.Entities;
using ML.AppBase;
using ML.Collections;
using MvvmCross.Core.ViewModels;
using MvvmCross.Platform;
using MvvmCross.Platform.Core;

namespace CrossBreed.ViewModels {
	public class ListsViewModel : BaseViewModel {
		public IObservableList<List> Lists { get; set; }

		public ListsViewModel(ICharacterManager characterManager, CharacterViewModels cache, CharacterListProvider characterListProvider) {
			var lists = new ObservableList<List>();
			IObservableList<CharacterViewModel> Mapper(IObservableList<Character> l) => new MappingObservableList<Character, CharacterViewModel>(l, cache.GetCharacterViewModel);
			lists.Add(new List(Strings.CharacterLists_Bookmarks, Mapper(new FilteringObservableList<Character, bool>(characterManager.OnlineCharacters, x => x.IsBookmarked, x => x))));
			lists.Add(new List(Strings.CharacterLists_Friends, Mapper(new FilteringObservableList<Character, bool>(characterManager.OnlineCharacters, x => x.IsFriend, x => x))));
			lists.Add(new List(Strings.CharacterLists_ChatOps, Mapper(new FilteringObservableList<Character, bool>(characterManager.OnlineCharacters, x => x.IsChatOp, x => x))));
			lists.InsertRange(characterListProvider.CustomLists.Select(l => new List(l.Name,
				Mapper(new FilteringObservableList<Character>(characterManager.OnlineCharacters, x => l.Characters.Contains(x.Name))))), lists.Count);
			Lists = lists;
		}

		public class List {
			public string Name { get; }
			public IObservableList<CharacterViewModel> Characters { get; }

			public List(string name, IObservableList<CharacterViewModel> characters) {
				Name = name;
				Characters = characters;
			}
		}
	}

	public class TabbedPeopleViewModel : BaseViewModel {
		public IReadOnlyList<Tab> Tabs { get; }
		public Tab SelectedTab { get; private set; }
		public ICommand TabSelectedCommand { get; }

		public CharacterViewModel SelectedCharacter { get; set; }

		public TabbedPeopleViewModel(ICharacterManager characterManager) {
			var cache = Mvx.GetSingleton<CharacterViewModels>();
			var nameComparer = Comparer<CharacterViewModel>.Create((x, y) => CultureInfo.CurrentUICulture.CompareInfo.Compare(x.Character.Name, y.Character.Name));
			var onlineCharacters = new MappingObservableList<Character, CharacterViewModel>(characterManager.OnlineCharacters, x => cache.GetCharacterViewModel(x));
			Tabs = new List<Tab> {
				new Tab(Strings.People_Friends, new UIThreadObservableList<CharacterViewModel>(new SortingObservableList<CharacterViewModel>(
					new FilteringObservableList<CharacterViewModel, bool>(onlineCharacters, x => x.Character.IsFriend, x => x), nameComparer))),
				new Tab(Strings.People_Bookmarks, new UIThreadObservableList<CharacterViewModel>(new SortingObservableList<CharacterViewModel>(
					new FilteringObservableList<CharacterViewModel, bool>(onlineCharacters, x => x.Character.IsBookmarked, x => x), nameComparer))),
				new Tab(Strings.People_All, new UIThreadObservableList<CharacterViewModel>(new SortingObservableList<CharacterViewModel>(onlineCharacters, nameComparer)))
			};
			SelectedTab = Tabs[0];
			TabSelectedCommand = new MvxCommand<Tab>(tab => {
				SelectedTab = tab;
				SelectedCharacter = null;
			});
		}

		public class Tab {
			private readonly CompareInfo compareInfo = CultureInfo.CurrentUICulture.CompareInfo;
			private readonly FilteringObservableList<CharacterViewModel, string> filteredList;
			public string Name { get; }
			public IObservableList<CharacterViewModel> Characters => filteredList;

			public string FilterText {
				set { filteredList.SetPredicate(string.IsNullOrEmpty(value) ? new Predicate<string>(x => true) : (x => compareInfo.IndexOf(x, value, CompareOptions.IgnoreCase) >= 0)); }
			}

			public Tab(string name, IObservableList<CharacterViewModel> characters) {
				Name = name;
				filteredList = new FilteringObservableList<CharacterViewModel, string>(characters, x => x.Character.Name, x => true);
			}
		}
	}

	public class ListPeopleViewModel : BaseViewModel {
		private string filterText;
		private readonly FilteringObservableList<Item, string> filteredList;
		private readonly CompareInfo compareInfo = CultureInfo.CurrentUICulture.CompareInfo;
		public IObservableList<Item> Characters { get; }

		public string FilterText {
			get => filterText;
			set {
				filterText = value;
				filteredList.SetPredicate(string.IsNullOrEmpty(value) ? new Predicate<string>(x => true) : (x => compareInfo.IndexOf(x, value, CompareOptions.IgnoreCase) >= 0));
			}
		}

		public ListPeopleViewModel(ICharacterManager characterManager) {
			var cache = Mvx.GetSingleton<CharacterViewModels>();
			filteredList = new FilteringObservableList<Item, string>(new MappingObservableList<Character, Item>(
				characterManager.OnlineCharacters, x => new Item(cache.GetCharacterViewModel(x))), x => x.Character.Character.Name, x => true);
			Characters = new UIThreadObservableList<Item>(new TrackingSortingObservableList<Item>(filteredList, Comparer<Item>.Create((x1, x2) => {
				var ranks = Comparer<int>.Default.Compare(x1.Rank, x2.Rank);
				return ranks != 0 ? ranks : Comparer<string>.Default.Compare(x1.Character.Character.Name, x2.Character.Character.Name);
			}), nameof(Item.Rank).SingletonEnumerable()));
		}

		public class Item : BaseViewModel {
			public int Rank { get; private set; }
			public CharacterViewModel Character { get; }

			public Item(CharacterViewModel character) {
				Character = character;
				character.CharacterLists.CollectionChanged += (sender, args) => CharacterRankChanged();
				character.Character.IsIgnoredChanged += CharacterRankChanged;
				CharacterRankChanged();
			}

			private void CharacterRankChanged() {
				Rank = Character.CharacterLists.ToList().Select(x => x.SortingOrder).DefaultIfEmpty(10).Min();
			}
		}
	}
}