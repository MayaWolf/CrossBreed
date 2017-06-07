using System.Collections.Generic;
using CrossBreed.Chat;
using CrossBreed.Entities;
using CrossBreed.Entities.ServerMessages;

namespace CrossBreed.BNC {
	public class CharacterManager: Chat.CharacterManager, IJoinMessageProvider {
		private ServerCommand frl;

		public CharacterManager(IChatManager chatManager, IApiManager apiManager) : base(chatManager, apiManager) {
			chatManager.CommandReceived += OnCommandReceived;
		}

		private void OnCommandReceived(ServerCommand serverCommand) {
			if(serverCommand.Type == ServerCommandType.FRL) frl = serverCommand;
		}

		public IEnumerable<ServerCommand> GetJoinCommands() {
			var onlineUsers = OnlineCharacters.ToList();
			yield return Helpers.CreateServerCommand(ServerCommandType.CON, new ServerCon { count = onlineUsers.Count });
			yield return frl;
			yield return Helpers.CreateServerCommand(ServerCommandType.IGN, new ServerIgn { characters = IgnoreList, action = "init" });
			yield return Helpers.CreateServerCommand(ServerCommandType.ADL, new ServerAdl { ops = OpsList });

			var list = new List<string[]>(100);
			foreach(var user in onlineUsers) {
				list.Add(new[] { user.Name, user.Gender.ToString(), user.Status.ToString(), user.StatusMessage });
				if(list.Count != 100) continue;
				yield return Helpers.CreateServerCommand(ServerCommandType.LIS, new ServerLis { characters = list });
				list.Clear();
			}
			if(list.Count > 0) {
				yield return Helpers.CreateServerCommand(ServerCommandType.LIS, new ServerLis { characters = list });
			}
		}
	}
}
