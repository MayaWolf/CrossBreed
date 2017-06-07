using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using CrossBreed.Chat;
using CrossBreed.Entities;
using CrossBreed.Entities.ApiResponses;
using ML.AppBase;
using MvvmCross.Core.ViewModels;
using MvvmCross.Platform;
using PropertyChanged;

namespace CrossBreed.ViewModels {
	public class FindPartnersViewModel : BaseViewModel {
		private readonly IChatManager chatManager;
		private readonly IApiManager apiManager;
		private readonly ICharacterManager characterManager;
		private readonly IMessageManager messageManager;
		private bool searchFieldsInitialized = false;

		public SearchFieldOptions SearchFields { get; set; } = new SearchFieldOptions();

		[DoNotNotify]
		public IReadOnlyList<SearchFieldOptions.Kink> SelectedKinks { get; set; }

		[DoNotNotify]
		public IReadOnlyList<string> SelectedGenders { get; set; }

		[DoNotNotify]
		public IReadOnlyList<string> SelectedRoles { get; set; }

		[DoNotNotify]
		public IReadOnlyList<string> SelectedOrientations { get; set; }

		[DoNotNotify]
		public IReadOnlyList<string> SelectedFurryPrefs { get; set; }

		[DoNotNotify]
		public IReadOnlyList<string> SelectedPositions { get; set; }

		[DoNotNotify]
		public IReadOnlyList<string> SelectedLanguages { get; set; }

		public ICommand SearchCommand { get; }
		public ICommand ViewProfileCommand { get; }
		public bool Pending { get; private set; }
		public IReadOnlyList<CharacterViewModel> Characters { get; private set; }

		public FindPartnersViewModel(IChatManager chatManager, IApiManager apiManager, ICharacterManager characterManager, IMessageManager messageManager) {
			this.chatManager = chatManager;
			this.apiManager = apiManager;
			this.characterManager = characterManager;
			this.messageManager = messageManager;
			if(!searchFieldsInitialized) GetSearchFields();
			SearchCommand = new MvxCommand(() => {
				Pending = true;
				chatManager.CommandReceived += OnCommandReceived;
				chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.FKS, new {
					kinks = SelectedKinks.Select(x => x.Id),
					genders = SelectedGenders,
					orientations = SelectedOrientations,
					positions = SelectedPositions,
					roles = SelectedRoles,
					languages = SelectedLanguages
				}));
			});
			ViewProfileCommand = new MvxCommand<CharacterViewModel>(c => Navigator.Navigate<CharacterViewModel, string>(c.Character.Name));
		}

		private void OnCommandReceived(ServerCommand serverCommand) {
			if(serverCommand.Type != ServerCommandType.FKS) return;
			chatManager.CommandReceived -= OnCommandReceived;
			var cache = Mvx.GetSingleton<CharacterViewModels>();
			Characters = serverCommand.Payload["characters"].Values<string>().Select(x => characterManager.GetCharacter(x)).Select(x => cache.GetCharacterViewModel(x)).ToList();
			Pending = false;
		}

		private async void GetSearchFields() {
			SearchFields = new SearchFieldOptions((await apiManager.QueryApi("mapping-list.php")).ToObject<MappingListResponse>());
		}

		public class SearchFieldOptions {
			public IReadOnlyList<Kink> Kinks { get; }
			public IReadOnlyList<string> Genders { get; }
			public IReadOnlyList<string> Roles { get; }
			public IReadOnlyList<string> Orientations { get; }
			public IReadOnlyList<string> Positions { get; }
			public IReadOnlyList<string> Languages { get; }
			public IReadOnlyList<string> FurryPrefs { get; }

			public SearchFieldOptions() { }

			public SearchFieldOptions(MappingListResponse response) {
				Kinks = response.kinks.Select(x => new Kink(x.Key, x.Value.name)).OrderBy(x => x.Name).ToList();
				Genders = response.listitems.Values.Where(x => x.name == "gender").Select(x => x.value).ToList();
				Roles = response.listitems.Values.Where(x => x.name == "subdom").Select(x => x.value).ToList();
				Orientations = response.listitems.Values.Where(x => x.name == "orientation").Select(x => x.value).ToList();
				Positions = response.listitems.Values.Where(x => x.name == "position").Select(x => x.value).ToList();
				Languages = response.listitems.Values.Where(x => x.name == "languagepreference").Select(x => x.value).ToList();
				FurryPrefs = response.listitems.Values.Where(x => x.name == "furrypref").Select(x => x.value).ToList();
			}

			public class Kink {
				public int Id { get; }
				public string Name { get; }

				public Kink(int id, string name) {
					Id = id;
					Name = name;
				}

				public override string ToString() => Name;
			}
		}
	}
}