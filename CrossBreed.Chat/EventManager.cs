using System;
using System.Collections.Specialized;
using System.Linq;
using CrossBreed.Entities;
using CrossBreed.Entities.ServerMessages;
using ML.Collections;

namespace CrossBreed.Chat {
	public class EventManager : IEventManager {
		private readonly ICharacterManager characterManager;
		private readonly IChannelManager channelManager;
		public event Action<Event> NewEvent;

		public EventManager(IChatManager chatManager, IMessageManager messageManager, ICharacterManager characterManager, IChannelManager channelManager) {
			this.characterManager = characterManager;
			this.channelManager = channelManager;
			chatManager.Connected += () => {
				characterManager.OnlineCharacters.CollectionChanged += (sender, args) => {
					switch(args.Action) {
						case NotifyCollectionChangedAction.Add:
							foreach(var character in args.NewItems.Cast<Character>()) AddEvent(new LoginEvent(character));
							break;
						case NotifyCollectionChangedAction.Remove:
							foreach(var character in args.OldItems.Cast<Character>()) AddEvent(new LogoutEvent(character));
							break;
					}
				};
				chatManager.CommandReceived += OnCommandReceived;
			};
			messageManager.ChannelMessageReceived += (channel, message) => {
				if(message.Sender == characterManager.OwnCharacter) return;
				OnChannelMessage(channel, message);
			};
			channelManager.JoinedChannels.CollectionChanged += (sender, args) => {
				switch(args.Action) {
					case NotifyCollectionChangedAction.Add:
						foreach(var channel in args.NewItems.Cast<Channel>()) {
							channel.Members.CollectionChanged += (_, memberArgs) => ChannelMembersChanged(channel, memberArgs);
						}
						break;
					case NotifyCollectionChangedAction.Remove:
						foreach(var channel in args.OldItems.Cast<Channel>()) {
							AddEvent(new ChannelLeaveEvent(channel, channel.Members[characterManager.OwnCharacter]));
						}
						break;
				}
			};
		}

		private void ChannelMembersChanged(Channel channel, NotifyCollectionChangedEventArgs args) {
			switch(args.Action) {
				case NotifyCollectionChangedAction.Add:
					foreach(var member in args.NewItems.Cast<Channel.Member>()) AddEvent(new ChannelJoinEvent(channel, member));
					break;
				case NotifyCollectionChangedAction.Remove:
					foreach(var member in args.OldItems.Cast<Channel.Member>()) {
						if(member.Character.Status != StatusEnum.Offline) AddEvent(new ChannelLeaveEvent(channel, member));
					}
					break;
				case NotifyCollectionChangedAction.Reset:
					AddEvent(new ChannelJoinEvent(channel, channel.Members[characterManager.OwnCharacter]));
					break;
			}
		}

		protected virtual void AddEvent(Event e) {
			NewEvent?.Invoke(e);
		}

		protected virtual void OnChannelMessage(Channel channel, Message message) {
			if(message.Text.Contains(characterManager.OwnCharacter.Name)) AddEvent(new MentionEvent(channel, message));
		}

		private void OnCommandReceived(ServerCommand command) {
			switch(command.Type) {
				case ServerCommandType.BRO:
					var bro = command.Payload.ToObject<ServerBro>();
					AddEvent(new BroadcastEvent(characterManager.GetCharacter(bro.character), bro.message, command.Time));
					break;
				case ServerCommandType.ERR:
					var err = command.Payload.ToObject<ServerErr>();
					AddEvent(new ErrorEvent(err.message, command.Time));
					break;
				case ServerCommandType.STA:
					var sta = command.Payload.ToObject<ServerSta>();
					var character = characterManager.GetCharacter(sta.character);
					AddEvent(new StatusEvent(character, sta.status, sta.statusmsg, command.Time));
					break;
				case ServerCommandType.RTB:
					var rtb = command.Payload.ToObject<ServerRtb>();
					switch(rtb.type) {
						case ServerRtb.Type.note:
							AddEvent(new NoteEvent(characterManager.GetCharacter(command.Value<string>("sender")), command.Value<string>("id"), command.Value<string>("subject")));
							break;
						case ServerRtb.Type.friendadd:
						case ServerRtb.Type.friendremove:
						case ServerRtb.Type.friendrequest:
						case ServerRtb.Type.trackadd:
						case ServerRtb.Type.trackrem:
							break;
						default:
							AddEvent(new RtbEvent(rtb.type, command.Value<string>("id"), characterManager.GetCharacter(rtb.name), command.Payload));
							break;
					}
					break;
				case ServerCommandType.CIU:
					var ciu = command.Payload.ToObject<ServerCiu>();
					var channel = (ciu.name == ciu.title ? channelManager.PublicChannels : channelManager.PrivateChannels)[ciu.name];
					AddEvent(new InviteEvent(characterManager.GetCharacter(ciu.sender), channel));
					break;
				case ServerCommandType.SYS:
					var chan = command.Value<string>("channel");
					AddEvent(new SysEvent(command.Value<string>("message"), channelManager.JoinedChannels.ContainsKey(chan) ? channelManager.JoinedChannels[chan] : null));
					break;
				case ServerCommandType.ZZZ:
					AddEvent(new SysEvent("Debug: " + command.Value<string>("message"), null));
					break;
			}
		}
	}
}