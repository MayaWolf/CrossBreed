using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CrossBreed.Chat;
using CrossBreed.Entities;
using ML.AppBase;
using ML.Collections;
using MvvmCross.Core.ViewModels;
using MvvmCross.Plugins.Messenger;

namespace CrossBreed.ViewModels {
	public class ChatViewModel : BaseViewModel<ChatViewModel.InitArgs> {
		private readonly IChatManager chatManager;
		private readonly IChannelManager channelManager;
		private readonly ICharacterManager characterManager;
		private readonly IMessageManager messageManager;
		private readonly ILogManager logManager;
		private readonly CharacterViewModels characterViewModels;
		private readonly MvxSubscriptionToken subscription;
		private readonly ObservableList<CharacterConversationViewModel> privateConversations = new ObservableList<CharacterConversationViewModel>();

		private TabViewModel selectedTab;
		public ICommand TabSelectedCommand { get; }

		public IObservableList<ChannelConversationViewModel> ChannelConversations { get; }
		public IObservableList<CharacterConversationViewModel> PrivateConversations { get; }
		public IObservableList<TabViewModel> AllConversations { get; }

		public MvxCommand<ConversationViewModel> CloseConversationCommand { get; }

		public TabViewModel SelectedTab {
			get => selectedTab;
			set {
				if(value == null) value = HomeTab;
				foreach(var item in AllConversations.ToList()) item.IsSelected = item == value;
				selectedTab = value;
			}
		}

		public event Action SelectedTabChanged;

		public HomeTabViewModel HomeTab { get; }

		public void ReloadState(State state) {
			ShowTab(state.SelectedChannel, state.SelectedCharacter);
		}

		public override async Task Initialize(InitArgs init) {
			ShowTab(init.Channel, init.Character);
		}

		private void ShowTab(string channel, string character) {
			TabViewModel tab;
			if(channel != null) tab = ChannelConversations.ToList().FirstOrDefault(x => x.Name == channel);
			else if(character != null) {
				tab = OpenConversation(character);
			} else tab = HomeTab;
			if(tab != null) SelectedTab = tab;
		}

		private CharacterConversationViewModel OpenConversation(string character) {
			var conversation = privateConversations.ToList().FirstOrDefault(x => x.Name == character);
			if(conversation == null) {
				conversation = new CharacterConversationViewModel(HandleCommand, messageManager, logManager, characterViewModels.GetCharacterViewModel(character));
				privateConversations.Add(conversation);
			}
			return conversation;
		}

		public State SaveState() {
			return new State {
				SelectedChannel = (SelectedTab as ChannelConversationViewModel)?.Name,
				SelectedCharacter = (SelectedTab as CharacterConversationViewModel)?.Name
			};
		}

		private bool HandleCommand(TabViewModel.Command command) {
			switch(command.Name) {
				case "uptime":
					chatManager.Send(new ClientCommand(ClientCommandType.UPT));
					return true;
				case "priv":
					OpenConversation(command.Text);
					return true;
				case "status":
					var index = command.Text.IndexOf(' ');
					if(!Enum.TryParse(command.Text.Substring(0, index), true, out StatusEnum status)) return false;
					if(status == StatusEnum.Idle || status == StatusEnum.Offline) return false;
					chatManager.SetStatus(status, command.Text.Substring(index + 1));
					return true;
				case "ignore":
				case "unignore":
					characterManager.SetIgnored(characterManager.GetCharacter(command.Text), command.Name == "ignore");
					return true;
				case "join":
					var name = command.Text;
					var channel = channelManager.PublicChannels.TryGet(name) ?? channelManager.PrivateChannels.TryGet(name);
					channelManager.JoinChannel(channel?.Id ?? name);
					return true;
				case "gkick":
					characterViewModels.GetCharacterViewModel(command.Text).KickCommand.Execute();
					return true;
				case "gban":
					characterViewModels.GetCharacterViewModel(command.Text).BanCommand.Execute();
					return true;
				case "gunban":
					chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.UNB, new { character = command.Text }));
					return true;
				case "gtimeout":
					var parameters = command.Text.Split(new[] { ',' }, 3);
					if(parameters.Length < 2) return false;
					chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.TMO,
						new { character = parameters[0].Trim(), time = parameters[1].Trim(), reason = parameters.Length == 3 ? parameters[2] : null }));
					return true;
				case "gop":
					chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.AOP, new { character = command.Text }));
					return true;
				case "gdeop":
					chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.DOP, new { character = command.Text }));
					return true;
				case "createchannel":
					chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.CRC, new { channel = command.Text }));
					return true;
				case "killchannel":
					chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.KIC, new { channel = command.Text }));
					return true;
				case "makeroom":
					channelManager.CreateChannel(command.Text);
					return true;
				case "broadcast":
					chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.BRO, new { message = command.Text }));
					return true;
				case "reward":
					chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.RWD, new { character = command.Text }));
					return true;
				case "reload":
					chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.RLD, new { save = command.Text }));
					return true;
				case "debug":
					var parts = command.Text.Split(new[] { ' ' }, 2);
					chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.ZZZ, new { command = parts[0], args = parts[1] }));
					return true;
				case "pendingreports":
					chatManager.Send(new ClientCommand(ClientCommandType.PCR));
					return true;
				default:
					return false;
			}
		}

		public ChatViewModel(IChatManager chatManager, IChannelManager channelManager, ICharacterManager characterManager, IMessageManager messageManager, IEventManager eventManager,
			ILogManager logManager, IMvxMessenger messenger, CharacterViewModels characterViewModels) {
			this.chatManager = chatManager;
			this.channelManager = channelManager;
			this.characterManager = characterManager;
			this.messageManager = messageManager;
			this.logManager = logManager;
			this.characterViewModels = characterViewModels;
			selectedTab = HomeTab = new HomeTabViewModel(HandleCommand, eventManager);
			HomeTab.IsSelected = true;
			ChannelConversations = new UIThreadObservableList<ChannelConversationViewModel>(new MappingObservableList<Channel, ChannelConversationViewModel>(
				channelManager.JoinedChannels, newChannel => {
					var settings = CharacterSettings.Instance;
					var recent = settings.RecentChannels.ToList();
					var index = recent.FindIndex(x => x.Item1 == newChannel.Id);
					if(index >= 0) recent.RemoveAt(index);
					recent.Insert(0, new Tuple<string, string>(newChannel.Id, newChannel.Name));
					settings.RecentChannels = recent;
					return new ChannelConversationViewModel(HandleCommand, messageManager, logManager, newChannel);
				}));
			PrivateConversations = new UIThreadObservableList<CharacterConversationViewModel>(privateConversations);
			PrivateConversations.CollectionChanged += (sender, args) => { return; };
			CloseConversationCommand = new MvxCommand<ConversationViewModel>(c => {
				if(c is ChannelConversationViewModel channel) channelManager.LeaveChannel(channel.Channel);
				else if(c is CharacterConversationViewModel character) privateConversations.Remove(character);
			});
			messageManager.CharacterMessageReceived += (character, message) => OpenConversation(character.Name);
			foreach(var character in CharacterSettings.Instance.PinnedCharacters) OpenConversation(character);
			AllConversations = new ConcatenatingObservableList<TabViewModel>(HomeTab.SingletonEnumerable(), ChannelConversations, PrivateConversations);

			TabSelectedCommand = new MvxCommand<TabViewModel>(tab => SelectedTab = tab);
			subscription = messenger.Subscribe<ShowTabMessage>(msg => ShowTab(msg.Channel, msg.Character));
		}

		public class State {
			public string SelectedChannel { get; set; }
			public string SelectedCharacter { get; set; }
		}

		public class InitArgs {
			public string Id { get; } = Guid.NewGuid().ToString();
			public string Channel { get; set; }
			public string Character { get; set; }
		}

		public class ShowTabMessage : MvxMessage {
			public ShowTabMessage(object sender) : base(sender) { }
			public string Channel { get; set; }
			public string Character { get; set; }
		}
	}
}