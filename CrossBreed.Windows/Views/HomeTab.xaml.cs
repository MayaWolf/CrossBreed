using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CrossBreed.ViewModels;
using ML.Settings;
using MvvmCross.Platform;
using ISettings = ML.Settings.ISettings;
using SettingsViewModel = ML.AppBase.SettingsViewModel;

namespace CrossBreed.Windows.Views {
	public partial class HomeTab {
		public FrameworkElement Settings { get; }
		public CharacterListsViewModel CharacterListsViewModel { get; }
		public RecentConversationsViewModel RecentConversationsViewModel { get; }

		public HomeTab() {
			RecentConversationsViewModel = Mvx.IocConstruct<RecentConversationsViewModel>();
			CharacterListsViewModel = Mvx.IocConstruct<CharacterListsViewModel>();
			Settings = new SettingsView { ViewModel = new SettingsViewModel(new SettingsImpl()) };
			InitializeComponent();
		}

		private class SettingsView : ML.AppBase.Windows.SettingsView {
			protected override void CreateSetting(ISetting setting, StackPanel panel, Thickness margin, string title, string summary) {
				if(setting is ISetting<Color>) {
					var colorPicker = new ColorPicker { SelectedColor = (Color) setting.Value, ToolTip = summary, Margin = new Thickness(0, 0, 10, 0) };
					colorPicker.SelectedColorChanged += color => setting.Value = color;
					panel.Children.Add(new StackPanel {
						Children = { colorPicker, new TextBlock { Text = title, ToolTip = summary } },
						Orientation = Orientation.Horizontal,
						Margin = margin
					});
				} else base.CreateSetting(setting, panel, margin, title, summary);
			}
		}

		private class SettingsImpl : ISettings {
			public IReadOnlyCollection<ISetting> AllSettings { get; }

			public IReadOnlyCollection<ISettings> Groups { get; }

			public string Key => null;

			public SettingsImpl() {
				ISettings settings = UserSettings.Instance;
				var groups = new List<ISettings>(settings.Groups.Count + 1);
				groups.AddRange(settings.Groups);
				groups.Add(ThemeSettings.Instance);
				Groups = groups;
				AllSettings = settings.AllSettings;
			}
		}
	}
}