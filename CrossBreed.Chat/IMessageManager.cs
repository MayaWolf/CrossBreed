using System;
using CrossBreed.Entities;
using ML.Collections;

namespace CrossBreed.Chat {
	public interface IMessageManager {
		event Action<Channel, Message> ChannelMessageReceived;
		event Action<Character, Message> CharacterMessageReceived;

		int MaxChatBytes { get; }
		int MaxAdBytes { get; }
		int MaxPrivateBytes { get; }

		bool CanSendMessages { get; }
		event Action CanSendMessagesChanged;

		void SendMessage(Channel channel, string message);
		void SendAd(Channel channel, string ad);
		void RollDice(Channel channel, string data);
		void SendMessage(Character character, string message);
		void RollDice(Character channel, string data);
		void UpdateTypingStatus(Character character, TypingStatusEnum status);
		Message GetPreviewMessage(Message.Type type, string text);
	}
}