using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Acr.Settings;
using CodeKicker.BBCode.SyntaxTree;
using CrossBreed.ViewModels;
using CrossBreed.Windows.Updater;
using ML.DependencyProperty;

namespace CrossBreed.Windows.Views {
	public partial class LoginView {
		[DependencyProperty]
		public UpdateData Update { get; set; }

		public LoginView() {
			InitializeComponent();
			new UpdateChecker().CheckForUpdate(App.Version).ContinueWith(task => {
				if(task.Result == null) return;
				Dispatcher.Invoke(() => Update = new UpdateData { Data = task.Result, Changelog = BbCodeParser.Parse(task.Result.ChangelogText) });
			});
			DataContextChanged += (s, e) => PasswordBox.Password = ViewModel.Password;
		}

		private void PasswordChanged(object sender, RoutedEventArgs e) {
			ViewModel.Password = ((PasswordBox) sender).Password;
		}

		private void CharacterClick(object sender, MouseButtonEventArgs e) {
			if(e.ClickCount != 2) return;
			var character = (LoginViewModel.CharacterListItem) ((FrameworkElement) sender).DataContext;
			var configPath = Path.Combine(App.DataDirectory, character.Name, "Settings.config");
			Settings.Current = new AppConfigSettingsImpl(configPath);
			if(!File.Exists(configPath) && SlimCatImporter.CanImportRoaming()) {
				if(System.Windows.MessageBox.Show(App.Current.MainWindow, WindowsStrings.SlimcatImport_Character,
						WindowsStrings.SlimcatImport_Title, MessageBoxButton.YesNo) == MessageBoxResult.Yes) {
					SlimCatImporter.ImportRoaming(character.Name);
					SlimCatImporter.ImportLogs(character.Name);
				}
			}
			ViewModel.CharacterSelected.Execute(character);
		}

		private async void DoUpdateButtonClicked(object sender, RoutedEventArgs e) {
			var directory = Path.GetTempPath();
			using(var client = new WebClient()) {
				var updateFile = Path.Combine(directory, "updater.exe");
				await client.DownloadFileTaskAsync(Update.Data.Patcher, updateFile);
				Process.Start(updateFile, $"{AppDomain.CurrentDomain.BaseDirectory} {Update.Data.Url} {Update.Data.FileHash}");
				App.Current.Shutdown();
			}
		}

		private void UpdateButtonClicked(object sender, RoutedEventArgs e) {
			UpdatePopup.IsOpen = true;
			e.Handled = true;
		}

		public class UpdateData {
			public LatestVersionData Data { get; set; }
			public SyntaxTreeNode Changelog { get; set; }
		}
	}
}