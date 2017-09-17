using System;
using System.Collections.Generic;
using System.Linq;
using CrossBreed.Entities;
using CrossBreed.Entities.ClientMessages;
using CrossBreed.Entities.ServerMessages;
using ML.Collections;

namespace CrossBreed.Chat {
	public class ChannelManager : IChannelManager {
		private readonly ObservableKeyedList<string, Channel> joinedChannels = new ObservableKeyedList<string, Channel>(x => x.Id);
		private readonly IChatManager chatManager;
		private readonly ICharacterManager characterManager;
		private readonly IDictionary<Channel, IDictionary<string, Channel.Member>> ops = new Dictionary<Channel, IDictionary<string, Channel.Member>>();
		private readonly IDictionary<Channel, ObservableKeyedList<Character, Channel.Member>> members = new Dictionary<Channel, ObservableKeyedList<Character, Channel.Member>>();
		private readonly ObservableKeyedList<string, ChannelListItem> publicChannels = new ObservableKeyedList<string, ChannelListItem>(x => x.Id);
		private readonly ObservableKeyedList<string, ChannelListItem> privateChannels = new ObservableKeyedList<string, ChannelListItem>(x => x.Id);

		public IObservableKeyedList<string, ChannelListItem> PublicChannels => publicChannels;
		public IObservableKeyedList<string, ChannelListItem> PrivateChannels => privateChannels;
		public IObservableKeyedList<string, Channel> JoinedChannels => joinedChannels;

		public ChannelManager(IChatManager chatManager, ICharacterManager characterManager) {
			this.chatManager = chatManager;
			this.characterManager = characterManager;
			chatManager.CommandReceived += OnCommandReceived;
		}

		protected IReadOnlyDictionary<string, Channel.Member> GetOps(Channel channel) => (IReadOnlyDictionary<string, Channel.Member>) ops[channel];

		public void JoinChannel(string channel) {
			chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.JCH, new ClientJch { channel = channel }));
		}

		public void LeaveChannel(Channel channel) {
			chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.LCH, new { channel = channel.Id }));
		}

		public void CreateChannel(string channel) {
			chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.CCR, new { channel }));
		}

		public void KickUser(Channel channel, string member) {
			chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.CKU, new { channel = channel.Id, character = member }));
		}

		public void SetUserBanned(Channel channel, string character, bool isBanned) {
			chatManager.Send(Helpers.CreateClientCommand(isBanned ? ClientCommandType.CBU : ClientCommandType.CUB, new { channel = channel.Id, character }));
		}

		public void TimeoutUser(Channel channel, string name, int minutes) {
			chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.CTU, new { channel = channel.Id, character = name, length = minutes }));
		}

		public void SetUserOp(Channel channel, string character, bool isOp) {
			chatManager.Send(Helpers.CreateClientCommand(isOp ? ClientCommandType.COA : ClientCommandType.COR, new { channel = channel.Id, character }));
		}

		public void SetOpen(Channel channel, bool isOpen) {
			chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.RST, new { channel = channel.Id, status = isOpen ? "public" : "private" }));
		}

		public void SetMode(Channel channel, Channel.ModeEnum mode) {
			chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.RMO, new { channel = channel.Id, mode }));
		}

		public void SetOwner(Channel channel, string name) {
			chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.CSO, new { channel = channel.Id, character = name }));
		}

		public void SetDescription(Channel channel, string description) {
			chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.CDS, new { channel = channel.Id, description }));
		}

		public void Invite(Channel channel, string character) {
			chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.CIU, new { channel = channel.Id, character }));
		}

		private void OnCommandReceived(ServerCommand command) {
			switch(command.Type) {
				case ServerCommandType.JCH:
					var jch = command.Payload.ToObject<ServerJch>();
					if(jch.character.identity == chatManager.OwnCharacterName) {
						var memberList = new ObservableKeyedList<Character, Channel.Member>(x => x.Character);
						var newChannel = new Channel(jch.channel.ToLower(), jch.title, memberList);
						members.Add(newChannel, memberList);
						joinedChannels.Add(newChannel);
						SetIsJoined(newChannel, true);
						return;
					}
					break;
				case ServerCommandType.ICH:
				case ServerCommandType.CDS:
				case ServerCommandType.CBU:
				case ServerCommandType.CKU:
				case ServerCommandType.COA:
				case ServerCommandType.COL:
				case ServerCommandType.COR:
				case ServerCommandType.CSO:
				case ServerCommandType.CTU:
				case ServerCommandType.LCH:
				case ServerCommandType.RMO:
					break;
				case ServerCommandType.FLN:
					var flnCharacter = command.Value<string>("character");
					foreach(var item in joinedChannels.ToList()) members[item].Remove(characterManager.GetCharacter(flnCharacter));
					return;
				case ServerCommandType.CHA:
					publicChannels.Reset(command.Payload.GetValue("channels").Select(x => {
						var name = x.Value<string>("name").ToLower();
						var item = new ChannelListItem(name, name, x.Value<int>("characters"));
						item.IsJoined = JoinedChannels.ContainsKey(item.Id);
						return item;
					}));
					return;
				case ServerCommandType.ORS:
					privateChannels.Reset(command.Payload.GetValue("channels").Select(x => {
						var item = new ChannelListItem(x.Value<string>("name").ToLower(), x.Value<string>("title"), x.Value<int>("characters"));
						item.IsJoined = JoinedChannels.ContainsKey(item.Id);
						return item;
					}));
					return;
				default:
					return;
			}
			HandleChannelMessage(joinedChannels[command.Value<string>("channel").ToLower()], command);
		}

		private Channel.Member GetChannelMember(Channel channel, string character) =>
			ops[channel].ContainsKey(character) ? ops[channel][character] : new Channel.Member(characterManager.GetCharacter(character));

		private IEnumerable<Channel.Member> GetOpList(ICollection<string> list) {
			var owner = list.First();
			if(owner != "") yield return new Channel.Member(characterManager.GetCharacter(owner)) { Rank = Channel.RankEnum.Owner };
			foreach(var item in list.Where(c => c != owner)) yield return new Channel.Member(characterManager.GetCharacter(item)) { Rank = Channel.RankEnum.Op };
		}

		private void HandleChannelMessage(Channel channel, ServerCommand msg) {
			switch(msg.Type) {
				case ServerCommandType.JCH:
					var character = msg.Payload["character"].Value<string>("identity");
					members[channel].Add(GetChannelMember(channel, character));
					ChangeListCount(channel, 1);
					break;
				case ServerCommandType.ICH:
					var ich = msg.Payload.ToObject<ServerIch>();
					channel.Mode = ich.mode;
					members[channel].Reset(ich.users.Select(c => GetChannelMember(channel, c.identity)));
					break;
				case ServerCommandType.CDS:
					channel.Description = msg.Value<string>("description");
					break;
				case ServerCommandType.CBU:
				case ServerCommandType.CKU:
				case ServerCommandType.CTU:
				case ServerCommandType.LCH:
					var channelCharacter = msg.Value<string>("character");
					if(channelCharacter == chatManager.OwnCharacterName) {
						joinedChannels.Remove(channel.Id);
						SetIsJoined(channel, false);
					} else members[channel].Remove(characterManager.GetCharacter(channelCharacter));
					ChangeListCount(channel, -1);
					break;
				case ServerCommandType.COA:
					var coaCharacter = msg.Value<string>("character");
					AddOp(channel, coaCharacter, Channel.RankEnum.Op);
					break;
				case ServerCommandType.COL:
					ops[channel] = GetOpList(msg.Payload["oplist"].Values<string>().ToList()).ToDictionary(x => x.Character.Name);
					break;
				case ServerCommandType.COR:
					var corCharacter = msg.Value<string>("character");
					ops[channel][corCharacter].Rank = Channel.RankEnum.User;
					ops[channel].Remove(corCharacter);
					break;
				case ServerCommandType.CSO:
					var owner = ops[channel].Values.FirstOrDefault(x => x.Rank == Channel.RankEnum.Owner);
					if(owner != null) owner.Rank = Channel.RankEnum.Op;
					var newOwner = msg.Value<string>("character");
					if(ops[channel].ContainsKey(newOwner)) ops[channel][newOwner].Rank = Channel.RankEnum.Owner;
					else AddOp(channel, newOwner, Channel.RankEnum.Owner);
					break;
				case ServerCommandType.RMO:
					channel.Mode = msg.Value<string>("mode").ToEnum<Channel.ModeEnum>();
					break;
			}
		}

		private void AddOp(Channel channel, string name, Channel.RankEnum rank) {
			var character = characterManager.GetCharacter(name);
			Channel.Member member;
			if(members[channel].ContainsKey(character)) {
				members[channel][character].Rank = rank;
				member = members[channel][character];
			} else member = new Channel.Member(character) { Rank = rank };
			ops[channel].Add(member.Character.Name, member);
		}

		public void RequestPublicChannels() {
			publicChannels.Clear();
			chatManager.Send(new ClientCommand(ClientCommandType.CHA));
		}

		public void RequestPrivateChannels() {
			privateChannels.Clear();
			chatManager.Send(new ClientCommand(ClientCommandType.ORS));
		}

		private void SetIsJoined(Channel channel, bool joined) {
			if(channel.Id == channel.Name) {
				if(publicChannels.ContainsKey(channel.Id)) publicChannels[channel.Id].IsJoined = joined;
			} else {
				if(privateChannels.ContainsKey(channel.Id)) privateChannels[channel.Id].IsJoined = joined;
			}
		}

		private void ChangeListCount(Channel channel, int diff) {
			if(channel.Id == channel.Name) {
				if(publicChannels.ContainsKey(channel.Id)) publicChannels[channel.Id].Count += diff;
			} else {
				if(privateChannels.ContainsKey(channel.Id)) privateChannels[channel.Id].Count += diff;
			}
		}
	}
}