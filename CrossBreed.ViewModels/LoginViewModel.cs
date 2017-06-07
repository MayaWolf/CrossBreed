using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using CrossBreed.Chat;
using ML.AppBase;
using MvvmCross.Core.ViewModels;
using MvvmCross.Platform;
using Newtonsoft.Json.Linq;

namespace CrossBreed.ViewModels {
	public class LoginViewModel : BaseViewModel {
		private IChatManager chatManager;

		public string LoginName { get; set; } = ClientSettings.Instance.UserName;
		public string Password { get; set; } = ClientSettings.Instance.Password;
		public string Host { get; set; } = ClientSettings.Instance.Host;
		public bool SaveLogin { get; set; } = ClientSettings.Instance.SaveLogin;
		public IReadOnlyList<CharacterListItem> Characters { get; private set; }
		public ICommand CharacterSelected { get; }
		public ICommand LoginCommand { get; }
		public bool LoggingIn { get; private set; }
		public bool Connecting { get; private set; }
		public string LoginError { get; private set; }
		public event Action ConnectingChanged;

		public LoginViewModel() {
			LoginCommand = new MvxCommand(LoginButtonPressed);
			CharacterSelected = new MvxCommand<CharacterListItem>(OnCharacterSelected);
		}

		private async void LoginButtonPressed() {
			LoggingIn = true;
			LoginError = null;
			var settings = ClientSettings.Instance;
			settings.SaveLogin = SaveLogin;
			if(SaveLogin) {
				settings.UserName = LoginName;
				settings.Password = Password;
				settings.Host = Host;
			}
			var apiManager = Mvx.GetSingleton<IApiManager>();
			if(CheckError(await apiManager.LogIn(LoginName, Password))) return;
			var response = await apiManager.QueryApi("character-list.php");
			if(CheckError(response)) return;
			LoggingIn = false;
			Characters = response["characters"].Values<string>().Select(x => new CharacterListItem { Name = x }).ToList();
		}

		private bool CheckError(JObject response) {
			if(response.TryGetValue("error", out JToken error) && !string.IsNullOrEmpty(error?.ToString())) {
				LoggingIn = false;
				LoginError = string.Format(this["Login_Error"], error);
				return true;
			}
			return false;
		}

		private void OnCharacterSelected(CharacterListItem value) {
			Connecting = true;
			chatManager = Mvx.GetSingleton<IChatManager>();

			chatManager.Connected += OnChatManagerConnected;
			chatManager.Connect(value.Name, Host);
		}

		private void OnChatManagerConnected() {
			Navigator.Navigate<MainViewModel>();
			Navigator.Close(this);
			chatManager.Connected -= OnChatManagerConnected;
		}

		public struct CharacterListItem {
			public string Name { get; set; }
			public string Image => CharacterViewModel.GetAvatar(Name);
		}
	}
}