using System.Collections.Generic;
using System.Linq;
using CrossBreed.Chat;
using CrossBreed.Entities;
using CrossBreed.Entities.ServerMessages;

namespace CrossBreed.BNC {
	public class ChannelManager: Chat.ChannelManager, IJoinMessageProvider {
		private readonly IChatManager chatManager;

		public ChannelManager(IChatManager chatManager, ICharacterManager characterManager) : base(chatManager, characterManager) {
			this.chatManager = chatManager;
		}

		private IEnumerable<string> GetOpNames(Channel channel) {
			var ops = GetOps(channel);
			if(ops.Values.All(x => x.Rank != Channel.RankEnum.Owner)) yield return "";
			foreach(var item in ops.Values.OrderByDescending(x => x.Rank).Select(x => x.Character.Name)) yield return item;
		}

		public IEnumerable<ServerCommand> GetJoinCommands() {
			foreach(var channel in JoinedChannels.ToList()) {
				yield return Helpers.CreateServerCommand(ServerCommandType.JCH, new ServerJch {
					channel = channel.Id, title = channel.Name, character = new ChannelUser { identity = chatManager.OwnCharacterName }
				});
				yield return Helpers.CreateServerCommand(ServerCommandType.COL, new ServerCol { channel = channel.Id, oplist = GetOpNames(channel).ToList() });
				var characters = channel.Members.ToList();
				yield return Helpers.CreateServerCommand(ServerCommandType.ICH, new ServerIch {
					channel = channel.Id, mode = channel.Mode, users = characters.Select(x => new ChannelUser { identity = x.Character.Name }).ToList()
				});
				yield return Helpers.CreateServerCommand(ServerCommandType.CDS, new ServerCds { channel = channel.Id, description = channel.Description });
			}
		}
	}
}
