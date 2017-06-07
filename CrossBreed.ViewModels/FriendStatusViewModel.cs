using System.Windows.Input;
using CrossBreed.Chat;
using MvvmCross.Core.ViewModels;
using System.Collections.Generic;
using System.Linq;
using CrossBreed.Entities.ApiResponses;
using ML.AppBase;

namespace CrossBreed.ViewModels {
	public class FriendStatusViewModel: BaseViewModel {
		private readonly IApiManager apiManager;
	    private readonly string remoteCharacter;
	    public string LocalCharacterName { get; }
        public string LocalCharacterImage => CharacterViewModel.GetAvatar(LocalCharacterName);
        public string RemoteCharacterImage => CharacterViewModel.GetAvatar(remoteCharacter);
		public int? RequestId { get; private set; }
		public StatusEnum Status { get; private set; }
		public NamedCommand Action1 { get; private set; }
		public NamedCommand Action2 { get; private set; }
		public string StatusText { get; private set; }

		public FriendStatusViewModel(IApiManager apiManager, string localCharacter, string remoteCharacter, int? requestId, StatusEnum status) {
			this.apiManager = apiManager;
			this.remoteCharacter = remoteCharacter;
			LocalCharacterName = localCharacter;
			RequestId = requestId;
			Init(status);
		}

		private void Init(StatusEnum status) {
			Status = status;
			switch(status) {
				case StatusEnum.None:
					Action1 = new NamedCommand(new MvxCommand(() => {
						Request($"request-send.php?source_name={LocalCharacterName}&dest_name={remoteCharacter}", StatusEnum.Outgoing);
					}), Strings.Character_FriendRequest);
					StatusText = string.Format(Strings.Profile_Friends_StatusNone, LocalCharacterName);
					break;
				case StatusEnum.Incoming:
					Action1 = new NamedCommand(new MvxCommand(() => Request($"request-accept.php?request_id={RequestId}", StatusEnum.Friends)), Strings.Character_FriendAccept);
					Action2 = new NamedCommand(new MvxCommand(() => Request($"request-deny.php?request_id={RequestId}", StatusEnum.None)), Strings.Character_FriendDeny);
					StatusText = string.Format(Strings.Profile_Friends_StatusIncoming, LocalCharacterName);
					break;
				case StatusEnum.Outgoing:
					Action1 = new NamedCommand(new MvxCommand(() => Request($"request-cancel.php?request_id={RequestId}", StatusEnum.None)), Strings.Character_FriendCancel);
					StatusText = string.Format(Strings.Profile_Friends_StatusOutgoing, LocalCharacterName);
					break;
				case StatusEnum.Friends:
					Action1 = new NamedCommand(new MvxCommand(() => {
						Request($"friend-remove.php?source_name={LocalCharacterName}&dest_name={remoteCharacter}", StatusEnum.None);
					}), Strings.Character_FriendDelete);
					StatusText = string.Format(Strings.Profile_Friends_StatusFriends, LocalCharacterName);
					break;
			}
		}

		private async void Request(string path, StatusEnum newStatus) {
			Action1 = null;
			Action2 = null;
			await apiManager.QueryApi(path);
			if(newStatus == StatusEnum.Outgoing) {
				var result = await apiManager.QueryApi("request-pending.php");
				RequestId = result["requests"].ToObject<IEnumerable<FriendListItem>>().First(x => x.source == LocalCharacterName && x.dest == remoteCharacter).id;
			}
			Init(newStatus);
		}

		public enum StatusEnum {
			None,
			Incoming,
			Outgoing,
			Friends
		}
	}

	public class NamedCommand {
		public ICommand Command { get; }
		public string Name { get; }

		public NamedCommand(ICommand command, string name) {
			Command = command;
			Name = name;
		}
	}
}