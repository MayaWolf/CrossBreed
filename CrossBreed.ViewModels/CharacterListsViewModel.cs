using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using ML.AppBase;
using ML.Collections;
using MvvmCross.Core.ViewModels;
using MvvmCross.Platform.UI;

namespace CrossBreed.ViewModels {
	public class CharacterListsViewModel : BaseViewModel {
		private readonly ObservableList<Item> customLists;

		public IReadOnlyList<Item> DefaultLists { get; }
		public IObservableList<Item> CustomLists => customLists;
		public IObservableList<Item> AllLists { get; }

		public ICommand SaveCommand { get; set; }
		public ICommand AddCommand { get; set; }

		public CharacterListsViewModel(CharacterListProvider provider) {
			customLists = new ObservableList<Item>(provider.ReloadCustomLists().Select(x => new Item(x)));
			DefaultLists = provider.ReloadDefaultLists().Select(x => new Item(x.Value, true) { Name = this["CharacterLists_" + x.Key] }).ToList();
			AllLists = new ConcatenatingObservableList<Item>(DefaultLists, customLists);
			SaveCommand = new MvxCommand(() => {
				provider.DefaultLists = DefaultLists.ToDictionary(x => x.List.Name, x => MapList(x, new CharacterList { Name = x.List.Name }));
				provider.SaveCustomLists(customLists.ToList().Select(x => {
					var oldList = x.List as CustomCharacterList;
					var list = MapList(x, new CustomCharacterList(oldList?.Id ?? Guid.NewGuid()) { Name = x.Name });
					if(oldList != null) list.Characters = oldList.Characters;
					return list;
				}).ToList());
			});
			AddCommand = new MvxCommand(() => customLists.Add(new Item { Name = "", SortOrder = 10, TextColor = new MvxColor(0), UnderlineColor = new MvxColor(0) }));
		}

		private static T MapList<T>(Item item, T list) where T : CharacterList {
			list.TextColor = (uint) item.TextColor.ARGB;
			list.UnderlineColor = (uint) item.UnderlineColor.ARGB;
			list.SortingOrder = item.SortOrder;
			list.HideAds = item.HideAds;
			list.HideInSearch = item.HideInSearch;
			return list;
		}

		public class Item : BaseViewModel {
			public string Name { get; set; }
			public MvxColor TextColor { get; set; }
			public MvxColor UnderlineColor { get; set; }
			public int SortOrder { get; set; }
			public bool HideAds { get; set; }
			public bool HideInSearch { get; set; }
			public bool IsDefault { get; internal set; }
			internal CharacterList List { get; }

			internal Item(CharacterList list, bool isDefault = false) {
				List = list;
				Name = list.Name;
				TextColor = new MvxColor(list.TextColor);
				UnderlineColor = new MvxColor(list.UnderlineColor);
				SortOrder = list.SortingOrder;
				HideAds = list.HideAds;
				HideInSearch = list.HideInSearch;
				IsDefault = isDefault;
			}

			internal Item() {}
		}
	}
}