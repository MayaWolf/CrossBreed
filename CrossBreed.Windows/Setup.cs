using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CrossBreed.Chat;
using CrossBreed.Net;
using CrossBreed.ViewModels;
using ML.AppBase;
using ML.AppBase.Windows;
using MvvmCross.Core.Navigation;
using MvvmCross.Core.ViewModels;
using MvvmCross.Core.Views;
using MvvmCross.Platform;
using MvvmCross.Platform.Core;
using MvvmCross.Wpf.Platform;
using MvvmCross.Wpf.Views;
using Newtonsoft.Json;
using Websockets;

namespace CrossBreed.Windows {
	public class Setup : MvxWpfSetup {
		private readonly IMvxViewDispatcher viewDispatcher;

		public Setup(Dispatcher dispatcher, IMvxWpfViewPresenter presenter) : base(dispatcher, presenter) {
			viewDispatcher = new MainThreadDispatcher(dispatcher, presenter);
		}

		protected override void InitializeFirstChance() {
			base.InitializeFirstChance();
			Mvx.ConstructAndRegisterSingleton<ICharacterListStorage, CharacterListStorage>();
			Mvx.ConstructAndRegisterSingleton<IDialogProvider, DialogProvider>();
			Mvx.RegisterSingleton<IResources>(new Resources(WindowsStrings.ResourceManager, Strings.ResourceManager, AppBaseStrings.ResourceManager));
		}

		protected override IMvxApplication CreateApp() {
			AppDomain.CurrentDomain.UnhandledException += HandleException;
			WebSocketFactory.Init(() => new WebSocketConnection());
			return new ViewModels.App();
		}

		protected override void InitializeLastChance() {
			base.InitializeLastChance();
			var navigationService = new NavigationService();
			Mvx.RegisterSingleton<IMvxNavigationService>(navigationService);
			Mvx.RegisterSingleton(navigationService);
			Mvx.Resolve<IChatManager>().Connected += () => {
				var themeSettings = ThemeSettings.Instance;
				var theme = App.Current.Theme;
				theme.FontSize = themeSettings.FontSize;
				theme.Background = new SolidColorBrush(themeSettings.BackgroundColor);
				theme.Foreground = new SolidColorBrush(themeSettings.ForegroundColor);
				theme.AccentForeground = new SolidColorBrush(themeSettings.AccentForegroundColor);
				theme.AccentColor = new SolidColorBrush(themeSettings.AccentColor);
				themeSettings.FontSizeChanged += () => theme.FontSize = themeSettings.FontSize;
				themeSettings.AccentColorChanged += () => theme.AccentColor = new SolidColorBrush(themeSettings.AccentColor);
				themeSettings.BackgroundColorChanged += () => theme.Background = new SolidColorBrush(themeSettings.BackgroundColor);
				themeSettings.ForegroundColorChanged += () => theme.Foreground = new SolidColorBrush(themeSettings.ForegroundColor);
				themeSettings.AccentForegroundColorChanged += () => { theme.AccentForeground = new SolidColorBrush(themeSettings.AccentForegroundColor); };
			};
			if(!File.Exists(Path.Combine(App.DataDirectory, "CrossBreed.config"))) {
				if(SlimCatImporter.CanImportLocal()) {
					if(MessageBox.Show(App.Current.MainWindow, WindowsStrings.SlimcatImport_Text, WindowsStrings.SlimcatImport_Title, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
						SlimCatImporter.ImportLocal();
					Process.Start(Application.ResourceAssembly.Location);
					Application.Current.Shutdown();
				}
			}
			var logSettings = UserSettings.Instance.Logging;
			if(logSettings.LogDirectory == null) {
				logSettings.LogDirectory = App.DataDirectory;
				Mvx.ConstructAndRegisterSingleton<ILogManager, LogManager>();
			}
		}

		private static void HandleException(object sender, UnhandledExceptionEventArgs args) {
			File.AppendAllText("crash.log", DateTime.Now + "\r\n" + args.ExceptionObject + "\r\n\r\n");
			if(MessageBox.Show(App.Current.MainWindow, WindowsStrings.Crash_Message, WindowsStrings.Crash_Title, MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes) {
				Process.Start("crash.log");
				Process.Start("https://www.f-list.net/read_notes.php?send=CrossBreed");
			}
			App.Current.Shutdown();
		}

		protected override IMvxViewDispatcher CreateViewDispatcher() => viewDispatcher;

		public class MainThreadDispatcher : MvxMainThreadDispatcher, IMvxViewDispatcher {
			private readonly Dispatcher dispatcher;
			private readonly IMvxViewPresenter presenter;

			public MainThreadDispatcher(Dispatcher dispatcher, IMvxViewPresenter presenter) {
				this.dispatcher = dispatcher;
				this.presenter = presenter;
			}

			public bool RequestMainThreadAction(Action action) {
				if(dispatcher.CheckAccess()) action();
				else dispatcher.InvokeAsync(() => ExceptionMaskedAction(action));
				return true;
			}

			public bool ShowViewModel(MvxViewModelRequest request) {
				dispatcher.Invoke(() => presenter.Show(request));
				return true;
			}

			public bool ChangePresentation(MvxPresentationHint hint) {
				dispatcher.Invoke(() => presenter.ChangePresentation(hint));
				return true;
			}
		}

		private class CharacterListStorage : ICharacterListStorage {
			public IReadOnlyCollection<CharacterList> DefaultLists {
				get {
					var value = GetConfiguration().AppSettings.Settings[nameof(DefaultLists)];
					return value == null ? null : JsonConvert.DeserializeObject<IReadOnlyCollection<CharacterList>>(value.Value);
				}
				set => WriteToSettings(nameof(DefaultLists), JsonConvert.SerializeObject(value, Formatting.None));
			}

			public IReadOnlyCollection<CustomCharacterList> CustomLists {
				get {
					var value = GetConfiguration().AppSettings.Settings[nameof(CustomLists)];
					return value == null ? null : JsonConvert.DeserializeObject<IReadOnlyCollection<CustomCharacterList>>(value.Value);
				}
				set => WriteToSettings(nameof(CustomLists), JsonConvert.SerializeObject(value, Formatting.None));
			}

			private static void WriteToSettings(string key, string value) {
				var config = GetConfiguration();
				var settings = config.AppSettings.Settings;
				var setting = settings[key];
				if(setting != null) setting.Value = value;
				else settings.Add(key, value);
				config.Save(ConfigurationSaveMode.Minimal);
			}

			private static Configuration GetConfiguration() {
				return ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap {
					ExeConfigFilename = Path.Combine(App.DataDirectory, "CrossBreed.config")
				}, ConfigurationUserLevel.None);
			}
		}
	}

	public class NavigationService : MvxNavigationService {
		private ChatViewModel chatViewModel;

		public override IMvxViewModel LoadViewModel<TViewModel>(IMvxBundle parameterValues = null, IMvxBundle savedState = null) {
			if(typeof(TViewModel) == typeof(ChatViewModel)) {
				return chatViewModel ?? (chatViewModel = (ChatViewModel) base.LoadViewModel<TViewModel>(parameterValues, savedState));
			}
			return base.LoadViewModel<TViewModel>(parameterValues, savedState);
		}

		public TViewModel Load<TViewModel>(IMvxBundle parameterValues = null) => (TViewModel) LoadViewModel<TViewModel>(parameterValues);
	}
}