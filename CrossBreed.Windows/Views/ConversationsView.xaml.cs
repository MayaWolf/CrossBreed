using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using CrossBreed.ViewModels;
using ML.Collections;
using ML.DependencyProperty;
using MvvmCross.Core.Navigation;
using MvvmCross.Platform;

namespace CrossBreed.Windows.Views {
	public partial class ConversationsView {
		public ConversationsView() {
			InitializeComponent();
		}

		[DependencyProperty(MetadataOptions = FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)]
		public TabViewModel SelectedTab { get; set; }

		[DependencyProperty(ChangedCallback = nameof(OnTabsSet))]
		public IObservableList<TabViewModel> Tabs { get; set; }

		private void ManageChannelsButtonClicked(object sender, RoutedEventArgs e) {
			Mvx.GetSingleton<IMvxNavigationService>().Navigate<ChannelsViewModel>();
		}

		private void OnTabsSet(DependencyPropertyChangedEventArgs args) {
			var conversationView = CollectionViewSource.GetDefaultView(Tabs);
			conversationView.GroupDescriptions.Add(new TypeGroupDescription());
			ListBox.ItemsSource = conversationView;
		}

		private class TypeGroupDescription : GroupDescription {
			private static readonly TabGroup ChannelsGroup = new TabGroup("Channels", true);
			private static readonly TabGroup CharactersGroup = new TabGroup("Private messages", false);

			public TypeGroupDescription() {
				GroupNames.Add(null);
				var setting = UserSettings.Instance.General.SortCharactersFirst;
				if(setting) GroupNames.Add(CharactersGroup);
				GroupNames.Add(ChannelsGroup);
				if(!setting) GroupNames.Add(CharactersGroup);
			}

			public override object GroupNameFromItem(object item, int level, CultureInfo culture) {
				if(item is ChannelConversationViewModel) return ChannelsGroup;
				if(item is CharacterConversationViewModel) return CharactersGroup;
				return null;
			}
		}

		public class TabGroup {
			public string Name { get; }
			public bool IsChannels { get; }

			public TabGroup(string name, bool isChannels) {
				Name = name;
				IsChannels = isChannels;
			}
		}
	}
}