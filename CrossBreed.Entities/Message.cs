using System;

namespace CrossBreed.Entities {
	public class Message {
		public string Text { get; }
		public Character Sender { get; }
		public DateTime Time { get; }
		public Type MessageType { get; }

		public Message(Type type, Character sender, DateTime time, string text) {
			MessageType = type;
			Text = text;
			Sender = sender;
			Time = time;
		}

		public enum Type {
			Message, Action, Ad, Roll
		}
	}
}