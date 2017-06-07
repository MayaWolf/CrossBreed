using System.Linq;
using CrossBreed.Chat;
using CrossBreed.Entities;

namespace CrossBreed.ViewModels {
	public class EventManager : Chat.EventManager {
		public EventManager(IChatManager chatManager, IMessageManager messageManager, ICharacterManager characterManager, IChannelManager channelManager) :
			base(chatManager, messageManager, characterManager, channelManager) { }

		protected override void AddEvent(Event e) {
			Character checkCharacter = null;
			if(e is StatusEvent statusEvent) checkCharacter = statusEvent.Character;
			else if(e is LoginEvent loginEvent) checkCharacter = loginEvent.Character;
			else if(e is LogoutEvent logoutEvent) checkCharacter = logoutEvent.Character;
			else if(e is ChannelJoinEvent joinEvent) checkCharacter = joinEvent.Member.Character;
			else if(e is ChannelLeaveEvent leaveEvent) checkCharacter = leaveEvent.Member.Character;
			if(checkCharacter != null && !checkCharacter.IsFriend && !checkCharacter.IsBookmarked) return; //TODO list
			base.AddEvent(e);
		}

		protected override void OnChannelMessage(Channel channel, Message message) {
			base.OnChannelMessage(channel, message);
			if(UserSettings.Instance.Notifications.HighlightWords.Any(message.Text.Contains)) base.AddEvent(new MentionEvent(channel, message));
		}
	}
}