using System;
using System.Net;
using System.Threading;
using CrossBreed.Entities;
using CrossBreed.Entities.ClientMessages;
using CrossBreed.Entities.ServerMessages;

namespace CrossBreed.Chat {
	public class MessageManager : IMessageManager {
		private readonly IChatManager chatManager;
		private readonly ICharacterManager characterManager;
		private readonly IChannelManager channelManager;
		private Timer canSendMessagesTimer;
		private double msgFlood;

		public event Action<Channel, Message> ChannelMessageReceived;
		public event Action<Character, Message> CharacterMessageReceived;

		public int MaxChatBytes { get; private set; }
		public int MaxAdBytes { get; private set; }
		public int MaxPrivateBytes { get; private set; }
		public bool CanSendMessages { get; private set; } = true;
		public event Action CanSendMessagesChanged;

		public MessageManager(IChatManager chatManager, ICharacterManager characterManager, IChannelManager channelManager) {
			this.chatManager = chatManager;
			this.characterManager = characterManager;
			this.channelManager = channelManager;
			chatManager.CommandReceived += HandleServerCommand;

			chatManager.Connected += () => {
				MaxChatBytes = chatManager.ServerVars["chat_max"].ToObject<int>();
				MaxAdBytes = chatManager.ServerVars["lfrp_max"].ToObject<int>();
				MaxPrivateBytes = chatManager.ServerVars["priv_max"].ToObject<int>();
				msgFlood = this.chatManager.ServerVars["msg_flood"].ToObject<double>();
				var pinned = AppSettings.Instance.PinnedChannels;
				if(pinned != null) {
					foreach(var channel in AppSettings.Instance.PinnedChannels) {
						chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.JCH, new ClientJch { channel = channel }));
					}
				}
			};
		}

		private Message CreateMessage(Character sender, DateTime time, string message) {
			var type = Message.Type.Message;
			if(message.StartsWith("/me") && !char.IsLetterOrDigit(message[3])) {
				type = Message.Type.Action;
				message = message.Substring(3);
			}
			return new Message(type, sender, time, message);
		}

		private void HandleServerCommand(ServerCommand command) {
			switch(command.Type) {
				case ServerCommandType.PRI:
					HandlePrivateMessage(command, command.Value<string>("character"));
					break;
				case ServerCommandType.MSG:
				case ServerCommandType.LRP:
					HandleChannelMessage(command, command.Value<string>("character"));
					break;
				case ServerCommandType.RLL:
					var rll = command.Payload.ToObject<ServerRll>();
					if(rll.channel == null) HandlePrivateMessage(command, rll.character);
					else HandleChannelMessage(command, rll.character);
					break;
				case ServerCommandType.TPN:
					var tpn = command.Payload.ToObject<ServerTpn>();
					characterManager.GetCharacter(tpn.character).TypingStatus = tpn.status;
					break;
			}
		}

		private void HandlePrivateMessage(ServerCommand command, string character) {
			var sender = characterManager.GetCharacter(character);
			sender.TypingStatus = TypingStatusEnum.Clear;
			if(sender.IsIgnored) {
				chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.IGN, new ClientIgn { action = ClientIgn.Action.notify, character = sender.Name }));
				return;
			}
			var message = command.Value<string>("message");
			switch(command.Type) {
				case ServerCommandType.PRI:
					CharacterMessageReceived?.Invoke(sender, CreateMessage(sender, command.Time, WebUtility.HtmlDecode(message)));
					break;
				case ServerCommandType.RLL:
					CharacterMessageReceived?.Invoke(sender, new Message(Message.Type.Roll, sender, command.Time, message.Substring(message.IndexOf("[/user]" + 7))));
					break;
			}
		}

		private void HandleChannelMessage(ServerCommand command, string character) {
			var sender = characterManager.GetCharacter(character);
			if(sender.IsIgnored) return;
			var channel = channelManager.JoinedChannels[command.Value<string>("channel")];
			var message = command.Value<string>("message");
			switch(command.Type) {
				case ServerCommandType.MSG:
					ChannelMessageReceived?.Invoke(channel, CreateMessage(sender, command.Time, WebUtility.HtmlDecode(message)));
					break;
				case ServerCommandType.LRP:
					ChannelMessageReceived?.Invoke(channel, new Message(Message.Type.Ad, sender, command.Time, WebUtility.HtmlDecode(message)));
					break;
				case ServerCommandType.RLL:
					ChannelMessageReceived?.Invoke(channel, new Message(Message.Type.Roll, sender, command.Time, message.Substring(message.IndexOf("[/user]" + 7))));
					break;
			}
		}
		
		private void SetCanSendMessages() {
			canSendMessagesTimer?.Dispose();
			CanSendMessages = false;
			CanSendMessagesChanged?.Invoke();
			canSendMessagesTimer = new Timer(state => {
				CanSendMessages = true;
				CanSendMessagesChanged?.Invoke();
				canSendMessagesTimer.Dispose();
			}, null, TimeSpan.FromSeconds(msgFlood), TimeSpan.Zero);
		}

		public void SendMessage(Channel channel, string message) {
			chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.MSG, new { channel = channel.Id, message }));
			ChannelMessageReceived?.Invoke(channel, CreateMessage(characterManager.OwnCharacter, DateTime.Now, message));
			SetCanSendMessages();
		}

		public void RollDice(Channel channel, string data) {
			chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.RLL, new { channel = channel.Id, dice = data }));
			SetCanSendMessages();
		}

		public void SendMessage(Character character, string message) {
			chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.PRI, new { recipient = character.Name, message }));
			CharacterMessageReceived?.Invoke(character, CreateMessage(characterManager.OwnCharacter, DateTime.Now, message));
			SetCanSendMessages();
		}

		public void RollDice(Character character, string data) {
			chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.RLL, new { recipient = character.Name, dice = data }));
			SetCanSendMessages();
		}

		public void UpdateTypingStatus(Character character, TypingStatusEnum status) {
			chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.TPN, new { character = character.Name, status }));
		}

		public void SendAd(Channel channel, string ad) {
			chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.LRP, new { channel = channel.Id, message = ad }));
			ChannelMessageReceived?.Invoke(channel, new Message(Message.Type.Ad, characterManager.OwnCharacter, DateTime.Now, ad));
		}

		public Message GetPreviewMessage(Message.Type type, string text) => new Message(type,characterManager.OwnCharacter, DateTime.Now, text);
	}
}