using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows.Input;
using CrossBreed.Chat;
using CrossBreed.Entities;
using ML.AppBase;
using ML.Collections;
using MvvmCross.Core.ViewModels;
using MvvmCross.Platform;
using MvvmCross.Platform.Core;

namespace CrossBreed.ViewModels {
	public class ChannelsViewModel : BaseViewModel {
		public IReadOnlyList<Tab> Tabs { get; }
		public ICommand TabSelectedCommand { get; }
		public Tab SelectedTab { get; set; }
		public ICommand RefreshCommand { get; }
		public ICommand CreateCommand { get; }

		public ChannelsViewModel(IChannelManager channelManager) {
			RefreshCommand = new MvxCommand(() => {
				channelManager.RequestPrivateChannels();
				channelManager.RequestPublicChannels();
			});
			if(channelManager.PublicChannels.Count == 0) RefreshCommand.Execute(null);
			Func<ChannelListItem, ListItem> mapper = item => new ListItem(channelManager, item);
			Tabs = new[] {
				new Tab(Strings.Channels_Public, new MappingObservableList<ChannelListItem, ListItem>(channelManager.PublicChannels, mapper)),
				new Tab(Strings.Channels_Private, new MappingObservableList<ChannelListItem, ListItem>(channelManager.PrivateChannels, mapper))
			};
			SelectedTab = Tabs[0];
			TabSelectedCommand = new MvxCommand<Tab>(tab => SelectedTab = tab);
			CreateCommand = new MvxCommand<string>(name => {
				channelManager.CreateChannel(name);
				channelManager.RequestPrivateChannels();
			});
		}
		
		public class Tab : BaseViewModel, IDisposable {
			private readonly CompareInfo compareInfo = CultureInfo.CurrentUICulture.CompareInfo;
			private readonly FilteringObservableList<ListItem, string> filteredList;
			private string filterText;
			public string Name { get; }
			public IObservableList<ListItem> Channels { get; }
			public bool IsLoading { get; private set; }

			public string FilterText {
				get => filterText;
				set {
					filteredList.SetPredicate(string.IsNullOrEmpty(value) ? new Predicate<string>(x => true) : (x => compareInfo.IndexOf(x, value, CompareOptions.IgnoreCase) >= 0));
					filterText = value;
				}
			}

			public Tab(string name, IObservableList<ListItem> channels) {
				Name = name;
				IsLoading = channels.Count == 0;
				filteredList = new FilteringObservableList<ListItem, string>(channels, x => x.Name, x => true);
				var sorted = new SortingObservableList<ListItem, int>(filteredList, x => x.Item.Count, Comparer<int>.Create((x1, x2) => -Comparer<int>.Default.Compare(x1, x2)));
				Channels = new UIThreadObservableList<ListItem>(sorted);
				Channels.CollectionChanged += ChannelsChanged;
			}

			private void ChannelsChanged(object sender, NotifyCollectionChangedEventArgs args) {
				IsLoading = Channels.Count == 0;
			}

			public void Dispose() {
				Channels.CollectionChanged -= ChannelsChanged;
			}
		}
		
		public class ListItem : BaseViewModel, IDisposable {
			private readonly IChannelManager channelManager;
			public ChannelListItem Item { get; }
			public bool JoinIsPending { get; private set; }
			public string Name => $"{Item.Name} ({Item.Count})";

			public bool IsJoined {
				get => Item.IsJoined;
				set {
					if(JoinIsPending) return;
					JoinIsPending = true;
					if(Item.IsJoined) channelManager.LeaveChannel(channelManager.JoinedChannels[Item.Id]);
					else channelManager.JoinChannel(Item.Id);
				}
			}

			public ListItem(IChannelManager channelManager, ChannelListItem item) {
				this.channelManager = channelManager;
				Item = item;
				item.IsJoinedChanged += ItemIsJoinedChanged;
			}

			private void ItemIsJoinedChanged() {
				Mvx.GetSingleton<IMvxMainThreadDispatcher>().RequestMainThreadAction(() => {
					JoinIsPending = false;
					RaisePropertyChanged(nameof(IsJoined));
				});
			}

			public void Dispose() {
				Item.IsJoinedChanged -= ItemIsJoinedChanged;
			}
		}
	}
}