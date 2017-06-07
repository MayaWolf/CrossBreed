using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CodeKicker.BBCode.SyntaxTree;
using CrossBreed.Chat;
using CrossBreed.Entities;
using ML.AppBase;
using ML.Collections;
using MvvmCross.Core.ViewModels;
using PropertyChanged;

namespace CrossBreed.ViewModels {
	public abstract class ConversationViewModel : TabViewModel {
		private readonly IMessageManager messageManager;
		public IObservableList<MessageViewModel> Messages { get; protected set; }
		public ICommand TogglePinCommand { get; }
		public bool IsPinned { get; protected set; }
		public bool CanSendMessage => messageManager.CanSendMessages;
		public override IMvxCommand SendCommand { get; }
		public abstract ICommand ShowSettings { get; }
		public int MaxMessageBytes { get; protected set; }
		public event Action MaxMessageBytesChanged;

		protected void AddToMessages(ObservableList<MessageViewModel> messages, MessageViewModel message) {
			if(messages.Count >= 100) messages.RemoveAt(0);
			messages.Add(message);
			if(!IsSelected) HasNew = true;
		}

		protected ConversationViewModel(Func<Command, bool> commandHandler, IMessageManager messageManager): base(commandHandler) {
			this.messageManager = messageManager;
			SendCommand = new MvxCommand(OnSend, () => CanSendMessage && !string.IsNullOrEmpty(EnteredText));
			messageManager.CanSendMessagesChanged += () => {
				SendCommand.RaiseCanExecuteChanged();
				RaisePropertyChanged(nameof(CanSendMessage));
			};
			TogglePinCommand = new MvxCommand(() => {
				SetPinned(!IsPinned);
				IsPinned = !IsPinned;
			});
		}

		protected abstract void SetPinned(bool pin);
	}
	
	public sealed class CharacterConversationViewModel : ConversationViewModel {
		private readonly IMessageManager messageManager;
		private Timer typingTimer;
		private readonly ObservableList<MessageViewModel> messages;
		private TypingStatusEnum currentTypingStatus;

		public CharacterViewModel Character { get; }
		public ICommand TypingCommand { get; }
		public override ICommand ShowSettings { get; }

		public override string Name => Character.Character.Name;
		public override string Image => Character.Image;

		public CharacterConversationViewModel(Func<Command, bool> commandHandler, IMessageManager messageManager, ILogManager logManager, CharacterViewModel character) :
			base(commandHandler, messageManager) {
			MaxMessageBytes = messageManager.MaxPrivateBytes;
			Character = character;
			this.messageManager = messageManager;
			messages = new ObservableList<MessageViewModel>(logManager.LoadReverse(logManager.GetLogId(character.Character), false, 20).Select(x => new MessageViewModel(x, true)));
			Messages = new UIThreadObservableList<MessageViewModel>(messages);
			messageManager.CharacterMessageReceived += (c, message) => {
				if(c == character.Character) AddToMessages(messages, new MessageViewModel(message));
			};
			EnteredTextChanged += () => SetTyping(!string.IsNullOrEmpty(EnteredText));
			IsPinned = CharacterSettings.Instance.PinnedCharacters.Contains(Name);
			TypingCommand = new MvxCommand<bool>(SetTyping);
		}

		private void SetTypingStatus(TypingStatusEnum status) {
			if(status == currentTypingStatus) return;
			messageManager.UpdateTypingStatus(Character.Character, status);
			currentTypingStatus = status;
		}

		private void SetTyping(bool typing) {
			if(!typing) {
				if(typingTimer != null) {
					typingTimer.Dispose();
					typingTimer = null;
				}
				SetTypingStatus(TypingStatusEnum.Clear);
			}
			else {
				SetTypingStatus(TypingStatusEnum.Typing);
				if(typingTimer != null) typingTimer.Change(3000, Timeout.Infinite);
				else typingTimer = new Timer(_ => SetTypingStatus(TypingStatusEnum.Paused), null, 3000, Timeout.Infinite);
			}
		}

		protected override bool OnSend(Command command) {
			switch(command.Name) {
				case null:
					messageManager.SendMessage(Character.Character, EnteredText);
					return true;
				case "clear":
					messages.Clear();
					return true;
				case "roll":
					messageManager.RollDice(Character.Character, command.Text);
					return true;
				case "bottle":
					messageManager.RollDice(Character.Character, "bottle");
					return true;
				default:
					return base.OnSend(command);
			}
		}

		protected override void SetPinned(bool pin) {
			var settings = CharacterSettings.Instance;
			if(pin) {
				settings.PinnedCharacters = new List<string>(settings.PinnedCharacters) { Name };
			} else {
				var list = new List<string>(settings.PinnedCharacters);
				list.Remove(Name);
				settings.PinnedCharacters = list;
			}
		}
	}
	
	public sealed class ChannelConversationViewModel : ConversationViewModel {
		private readonly IMessageManager messageManager;
		private readonly IChannelManager channelManager;
		private Timer timer;
		private bool isShowingAds;
		private readonly ObservableList<MessageViewModel> ads = new ObservableList<MessageViewModel>(), messages;
		private readonly UIThreadObservableList<MessageViewModel> messagesWrapper;
		private bool autoPostAds;
		public Channel Channel { get; }

		public override string Name => Channel.Name;
		public override string Image => "res:ic_dashboard_black_24dp";
		public SyntaxTreeNode FormattedDescription => BbCodeParser.Parse(Channel.Description);
		public string ModeText => IsShowingAds ? Strings.Channel_ModeAds : Strings.Channel_ModeChat;

		public ICommand ShowMembersCommand { get; }
		public override ICommand ShowSettings { get; }

		public bool AutoPostAds {
			get => autoPostAds;
			set {
				autoPostAds = value;
				if(!value) {
					timer?.Dispose();
					IsAutoPosting = false;
				}
			}
		}
		
		[AlsoNotifyFor(nameof(ModeText), nameof(ToggleAdsCommandName))]
		public bool IsShowingAds {
			get => isShowingAds;
			set {
				isShowingAds = value;
				messagesWrapper.Reset(value ? ads : messages);
				MaxMessageBytes = value ? messageManager.MaxAdBytes : messageManager.MaxChatBytes;
			}
		}

		public bool IsAutoPosting { get; private set; }
		public event Action IsAutoPostingChanged;

		public IMvxCommand ToggleAdsCommand { get; }
		public string ToggleAdsCommandName => IsShowingAds ? Strings.Chat_ShowMessages : Strings.Chat_ShowAds;

		public ChannelConversationViewModel(Func<Command, bool> commandHandler, IMessageManager messageManager, ILogManager logManager, Channel channel) :
			base(commandHandler, messageManager) {
			this.messageManager = messageManager;
			Channel = channel;

			messages = new ObservableList<MessageViewModel>(logManager.LoadReverse(logManager.GetLogId(channel), false, 20).Select(x => new MessageViewModel(x, true)));
			messagesWrapper = new UIThreadObservableList<MessageViewModel>(messages);
			Messages = messagesWrapper;
			MaxMessageBytes = messageManager.MaxChatBytes;
			IsShowingAds = channel.Mode == Channel.ModeEnum.Ads;

			channel.ModeChanged += () => {
				if(channel.Mode == Channel.ModeEnum.Ads) IsShowingAds = true;
				else if(channel.Mode == Channel.ModeEnum.Chat) IsShowingAds = false;
				ToggleAdsCommand.RaiseCanExecuteChanged();
			};
			messageManager.ChannelMessageReceived += (c, message) => {
				if(c == channel) AddToMessages(message.MessageType == Message.Type.Ad ? ads : messages, new MessageViewModel(message));
			};
			ShowMembersCommand = new MvxCommand(() => Navigator.Navigate<ChannelMembersViewModel, string>(channel.Id));
			IsPinned = AppSettings.Instance.PinnedChannels.Contains(channel.Id);
			ToggleAdsCommand = new MvxCommand(() => IsShowingAds = !IsShowingAds, () => channel.Mode == Channel.ModeEnum.Both);
		}

		private void SendAd() {
			messageManager.SendAd(Channel, EnteredText);
		}

		protected override void SetPinned(bool pin) {
			var settings = AppSettings.Instance;
			if(pin) {
				settings.PinnedChannels = new List<string>(settings.PinnedChannels) { Channel.Id };
			} else {
				var list = new List<string>(settings.PinnedChannels);
				list.Remove(Channel.Id);
				settings.PinnedChannels = list;
			}
		}


		protected override bool OnSend(Command command) {
			switch(command.Name) {
				case null:
					if(IsShowingAds) {
						SendAd();
						if(AutoPostAds) {
							IsAutoPosting = true;
							var time = TimeSpan.FromSeconds(605);
							timer = new Timer(state => SendAd(), null, time, time);
						}
						return true;
					}
					messageManager.SendMessage(Channel, EnteredText);
					return true;
				case "clear":
					messages.Clear();
					ads.Clear();
					return true;
				case "roll":
					messageManager.RollDice(Channel, command.Text);
					return true;
				case "bottle":
					messageManager.RollDice(Channel, "bottle");
					return true;
				case "openroom":
					channelManager.SetOpen(Channel, true);
					return true;
				case "closeroom":
					channelManager.SetOpen(Channel, false);
					return true;
				case "setmode":
					if(!Enum.TryParse(command.Text, true, out Channel.ModeEnum mode)) return false;
					channelManager.SetMode(Channel, mode);
					return true;
				case "cop":
					channelManager.SetUserOp(Channel, command.Text, true);
					return true;
				case "cdeop":
					channelManager.SetUserOp(Channel, command.Text, false);
					return true;
				case "setowner":
					channelManager.SetOwner(Channel, command.Text);
					return true;
				case "setdescription":
					channelManager.SetDescription(Channel, command.Text);
					return true;
				case "invite":
					channelManager.Invite(Channel, command.Text);
					return true;
				case "kick":
					channelManager.KickUser(Channel, command.Text);
					return true;
				case "ban":
					channelManager.SetUserBanned(Channel, command.Text, true);
					return true;
				case "unban":
					channelManager.SetUserBanned(Channel, command.Text, false);
					return true;
				case "timeout":
					var index = command.Text.IndexOf(',');
					if(!int.TryParse(command.Text.Substring(index + 1).Trim(), out var minutes)) return false;
					channelManager.TimeoutUser(Channel, command.Text.Substring(index).Trim(), minutes);
					return true;
				default:
					return base.OnSend(command);
			}
		}
	}
}