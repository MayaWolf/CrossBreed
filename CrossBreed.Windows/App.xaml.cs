using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Acr.Settings;
using CrossBreed.Chat;
using CrossBreed.Entities;
using CrossBreed.ViewModels;
using ML.Collections;
using MvvmCross.Core.ViewModels;
using MvvmCross.Platform;
using MvvmCross.Wpf.Views;

namespace CrossBreed.Windows {
	public partial class App {
		public const int Version = 2;
		public static readonly string DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CrossBreed");

		private bool setupComplete;
		public new static App Current => (App) Application.Current;
		public ThemeResources Theme { get; private set; }

		static App() {
			Settings.Current = new GlobalSettings(Path.Combine(DataDirectory, "CrossBreed.config"));
		}

		protected override void OnActivated(EventArgs e) {
			if(!setupComplete) {
				Theme = new ThemeResources(Resources);
				new Setup(Dispatcher, new Presenter(MainWindow)).Initialize();
				Mvx.Resolve<IMvxAppStart>().Start();
				setupComplete = true;
			}
			base.OnActivated(e);
		}

		private class Presenter : MvxSimpleWpfViewPresenter {
			public Presenter(ContentControl contentControl) : base(contentControl) { }

			public override void Show(MvxViewModelRequest request) {
				if(request.ViewModelType == typeof(ChatViewModel)) return;
				base.Show(request);
			}

			public override void Present(FrameworkElement frameworkElement) {
				if(frameworkElement is Views.ChannelsView) {
					new Window { Content = frameworkElement, Title = Strings.Tab_Channels, Width = 300, Height = 500 }.Show();
					return;
				}
				if(frameworkElement is Views.ProfileView profile) {
					new Window { Content = frameworkElement, Title = profile.ViewModel.Character.Name, Width = 800, Height = 600 }.Show();
					return;
				}
				if(frameworkElement is Views.LogsView) {
					new Window { Content = frameworkElement, Title = Strings.Tab_Logs, Width = 800, Height = 600 }.Show();
					return;
				}
				base.Present(frameworkElement);
			}
		}

		public class GlobalSettings : AppConfigSettingsImpl {
			private readonly string fileName;

			public GlobalSettings(string fileName) : base(fileName) {
				this.fileName = fileName;
			}

			protected override void NativeSet(Type type, string key, object value) {
				var config = GetConfiguration();
				var settings = config.AppSettings.Settings;
				var configurationElement = settings[key];
				if(configurationElement == null) settings.Add(key, Serialize(type, value));
				else configurationElement.Value = Serialize(type, value);
				config.Save(ConfigurationSaveMode.Minimal);
			}

			protected override void Flush() {}

			protected override void NativeRemove(string[] keys) {
				var config = GetConfiguration();
				foreach(var key in keys) config.AppSettings.Settings.Remove(key);
				config.Save(ConfigurationSaveMode.Minimal);
			}

			private Configuration GetConfiguration() =>
				ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap { ExeConfigFilename = fileName }, ConfigurationUserLevel.None);
		}

		public class ThemeResources {
			private const string prefix = "Theme.";
			private readonly IDictionary dict;

			public double FontSize {
				get => (double) dict[prefix + nameof(FontSize)];
				set {
					dict[prefix + nameof(FontSize)] = value;
					FontSizeBig = value + 4;
					FontSizeSmall = value - 4;
				}
			}

			public double FontSizeBig {
				get => (double) dict[prefix + nameof(FontSizeBig)];
				private set => dict[prefix + nameof(FontSizeBig)] = value;
			}

			public double FontSizeSmall {
				get => (double) dict[prefix + nameof(FontSizeSmall)];
				private set => dict[prefix + nameof(FontSizeSmall)] = value;
			}

			public SolidColorBrush Foreground {
				get => (SolidColorBrush) dict[prefix + nameof(Foreground)];
				set {
					dict[prefix + nameof(Foreground)] = value;
					ForegroundLight = new SolidColorBrush(value.Color) { Opacity = 0.4 };
				}
			}

			public SolidColorBrush ForegroundLight {
				get => (SolidColorBrush) dict[prefix + nameof(ForegroundLight)];
				set => dict[prefix + nameof(ForegroundLight)] = value;
			}

			public SolidColorBrush Background {
				get => (SolidColorBrush) dict[prefix + nameof(Background)];
				set => dict[prefix + nameof(Background)] = value;
			}

			public SolidColorBrush AccentColor {
				get => (SolidColorBrush) dict[prefix + nameof(AccentColor)];
				set {
					dict[prefix + nameof(AccentColor)] = value;
					AccentColorLight = new SolidColorBrush(value.Color) { Opacity = 0.5 };
				}
			}

			public SolidColorBrush AccentColorLight {
				get => (SolidColorBrush) dict[prefix + nameof(AccentColorLight)];
				set => dict[prefix + nameof(AccentColorLight)] = value;
			}

			public SolidColorBrush AccentForeground {
				get => (SolidColorBrush) dict[prefix + nameof(AccentForeground)];
				set => dict[prefix + nameof(AccentForeground)] = value;
			}

			public ThemeResources(IDictionary dict) {
				this.dict = dict;
			}
		}
	}
}