using CrossBreed.Entities;
using ML.Collections;

namespace CrossBreed.Chat {
	public interface IChannelManager {
		IObservableKeyedList<string, Channel> JoinedChannels { get; }
		IObservableKeyedList<string, ChannelListItem> PrivateChannels { get; }
		IObservableKeyedList<string, ChannelListItem> PublicChannels { get; }

		void JoinChannel(string channel);
		void LeaveChannel(Channel channel);
		void RequestPrivateChannels();
		void RequestPublicChannels();
		void CreateChannel(string name);

		void KickUser(Channel channel, string member);
		void SetUserBanned(Channel channel, string name, bool isBanned);
		void TimeoutUser(Channel channel, string name, int minutes);
		void SetUserOp(Channel channel, string name, bool isOp);
		void SetOpen(Channel channel, bool isOpen);
		void SetMode(Channel channel, Channel.ModeEnum mode);
		void SetOwner(Channel channel, string name);
		void SetDescription(Channel channel, string description);
		void Invite(Channel channel, string character);
	}
}