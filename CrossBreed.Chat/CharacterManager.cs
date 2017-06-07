using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CrossBreed.Entities;
using CrossBreed.Entities.ClientMessages;
using CrossBreed.Entities.ServerMessages;
using ML.Collections;

namespace CrossBreed.Chat {
	public class CharacterManager : ICharacterManager {
		private readonly IChatManager chatManager;
		private readonly IApiManager apiManager;
		private readonly ManualResetEvent gotFriends = new ManualResetEvent(true);
		private List<string> ignoreList, opsList, bookmarkList;
		private readonly IDictionary<string, int> friendCounts = new Dictionary<string, int>();

		private readonly ObservableKeyedList<string, Character> characterList = new ObservableKeyedList<string, Character>(x => x.Name);
		public IObservableList<Character> OnlineCharacters { get; }
		public Character OwnCharacter => characterList[chatManager.OwnCharacterName];

		protected IReadOnlyCollection<string> IgnoreList => ignoreList;
		protected IReadOnlyCollection<string> OpsList => opsList;
		protected IReadOnlyCollection<string> BookmarkList => bookmarkList;

		public CharacterManager(IChatManager chatManager, IApiManager apiManager) {
			this.chatManager = chatManager;
			this.apiManager = apiManager;
			OnlineCharacters = new FilteringObservableList<Character, StatusEnum>(characterList, x => x.Status, x => x != StatusEnum.Offline);
			chatManager.CommandReceived += HandleServerCommand;
		}

		private async void GetBookmarksAndFriends(IApiManager apiManager) {
			var result = await apiManager.QueryApi("bookmark-list.php");
			bookmarkList = result["characters"].Values<string>().ToList();

			result = await apiManager.QueryApi("friend-list.php");
			foreach(var friend in result["friends"].Children()) {
				var name = friend.Value<string>("dest");
				int value;
				if(!friendCounts.TryGetValue(name, out value)) friendCounts.Add(name, 1);
				else friendCounts[name] = value + 1;
			}

			gotFriends.Set();
		}

		public Character GetCharacter(string name) =>
		    characterList.ContainsKey(name) ? characterList[name] : CreateCharacter(name);

		private Character CreateCharacter(string name) {
			var character = new Character(name) {
				IsIgnored = ignoreList.Contains(name),
				IsChatOp = opsList.Contains(name),
				IsBookmarked = bookmarkList.Contains(name),
				IsFriend = friendCounts.ContainsKey(name)
			};
			characterList.Add(character);
			return character;
		}

		private void AddCharacter(string name, GenderEnum gender, StatusEnum status, string statusMessage = null) {
			var character = GetCharacter(name);
			character.Gender = gender;
			character.Status = status;
			character.StatusMessage = statusMessage;
		}

		private void HandleServerCommand(ServerCommand msg) {
			gotFriends.WaitOne();
			switch(msg.Type) {
				case ServerCommandType.HLO:
					gotFriends.Reset();
					GetBookmarksAndFriends(apiManager);
					break;
				case ServerCommandType.IGN:
					var ign = msg.Payload.ToObject<ServerIgn>();
					switch(ign.action) {
						case "init":
							ignoreList = ign.characters.ToList();
							break;
						case "add":
							ignoreList.Add(ign.character);
							if(characterList.ContainsKey(ign.character)) {
								characterList[ign.character].IsIgnored = true;
							}
							break;
						case "remove":
							ignoreList.Remove(ign.character);
							if(characterList.ContainsKey(ign.character)) {
								characterList[ign.character].IsIgnored = false;
							}
							break;
					}
					break;
				case ServerCommandType.ADL:
					opsList = msg.Payload["ops"].Values<string>().ToList();
					break;
				case ServerCommandType.LIS:
					var characters = msg.Payload.ToObject<ServerLis>().characters;
					foreach(var user in characters) AddCharacter(user[0], Helpers.GetGender(user[1]), user[2].ToEnum<StatusEnum>(), user[3]);
					break;
				case ServerCommandType.FLN:
					characterList[msg.Value<string>("character")].Status = StatusEnum.Offline;
					break;
				case ServerCommandType.NLN:
					var nln = msg.Payload.ToObject<ServerNln>();
					AddCharacter(nln.identity, nln.gender, nln.status);
					break;
				case ServerCommandType.STA:
					var sta = msg.Payload.ToObject<ServerSta>();
					var character = characterList[sta.character];
					character.Status = sta.status;
					character.StatusMessage = sta.statusmsg;
					break;
				case ServerCommandType.AOP:
					var aopCharacter = msg.Payload.ToObject<ServerAop>().character;
					opsList.Add(aopCharacter);
					if(characterList.ContainsKey(aopCharacter)) {
						characterList[aopCharacter].IsChatOp = true;
					}
					break;
				case ServerCommandType.DOP:
					var dopCharacter = msg.Payload.ToObject<ServerDop>().character;
					opsList.Remove(dopCharacter);
					if(characterList.ContainsKey(dopCharacter)) {
						characterList[dopCharacter].IsChatOp = false;
					}
					break;
				case ServerCommandType.RTB:
					var rtb = msg.Payload.ToObject<ServerRtb>();
					switch(rtb.type) {
						case ServerRtb.Type.trackadd:
							characterList[rtb.name].IsBookmarked = true;
							break;
						case ServerRtb.Type.trackrem:
							characterList[rtb.name].IsBookmarked = false;
							break;
						case ServerRtb.Type.friendadd:
							int addCount;
							if(friendCounts.TryGetValue(rtb.name, out addCount)) friendCounts[rtb.name] = addCount + 1;
							else {
								friendCounts.Add(rtb.name, 1);
								characterList[rtb.name].IsFriend = true;
							}
							break;
						case ServerRtb.Type.friendremove:
							var removeCount = friendCounts[rtb.name];
							if(removeCount > 1) friendCounts[rtb.name] = removeCount - 1;
							else {
								friendCounts.Remove(rtb.name);
								characterList[rtb.name].IsFriend = false;
							}
							break;
					}
					break;
			}
		}

	    public void SetIgnored(Character character, bool ignored) {
	        var action = ignored ? ClientIgn.Action.delete : ClientIgn.Action.add;
	        chatManager.Send(Helpers.CreateClientCommand(ClientCommandType.IGN, new ClientIgn { action = action, character = character.Name }));
        }
	}
}