using System;
using CrossBreed.Entities.ServerMessages;
using Newtonsoft.Json.Linq;

namespace CrossBreed.Entities {
	public class Event {
		public DateTime Time { get; }

		protected Event(DateTime? time = null) {
			Time = time ?? DateTime.Now;
		}
	}

	public class MentionEvent : Event {
		public Channel Channel { get; }
		public Message Message { get; }

		public MentionEvent(Channel channel, Message message, DateTime? time = null) : base(time) {
			Channel = channel;
			Message = message;
		}
	}

	public class LoginEvent : Event {
		public Character Character { get; }

		public LoginEvent(Character character, DateTime? time = null) : base(time) {
			Character = character;
		}
	}

	public class LogoutEvent : Event {
		public Character Character { get; }

		public LogoutEvent(Character character, DateTime? time = null) : base(time) {
			Character = character;
		}
	}

	public class BroadcastEvent : Event {
		public Character Character { get; }
		public string Message { get; }

		public BroadcastEvent(Character character, string message, DateTime? time = null) : base(time) {
			Character = character;
			Message = message;
		}
	}

	public class NoteEvent : Event {
		public Character Character { get; }
		public string Id { get; }
		public string Title { get; }

		public NoteEvent(Character character, string id, string title, DateTime? time = null) : base(time) {
			Id = id;
			Character = character;
			Title = title;
		}
	}

	public class ErrorEvent : Event {
		public string Message { get; }
		public ErrorEvent(string message, DateTime? time = null) : base(time) {
			Message = message;
		}
	}

	public class StatusEvent : Event {
		public Character Character { get; }
		public StatusEnum Status { get; }
		public string StatusMessage { get; }
		public StatusEvent(Character character, StatusEnum status, string statusMessage, DateTime? time = null) : base(time) {
			Character = character;
			Status = status;
			StatusMessage = statusMessage;
		}
	}

	public class ChannelJoinEvent : Event {
		public Channel Channel { get; }
		public Channel.Member Member { get; }

		public ChannelJoinEvent(Channel channel, Channel.Member member, DateTime? time = null) : base(time) {
			Channel = channel;
			Member = member;
		}
	}
	public class ChannelLeaveEvent : Event {
		public Channel Channel { get; }
		public Channel.Member Member { get; }

		public ChannelLeaveEvent(Channel channel, Channel.Member member, DateTime? time = null) : base(time) {
			Channel = channel;
			Member = member;
		}
	}

	public class InviteEvent : Event {
		public Character Sender { get; }
		public ChannelListItem Channel { get; }

		public InviteEvent(Character sender, ChannelListItem channel, DateTime? time = null) : base(time) {
			Sender = sender;
			Channel = channel;
		}
	}

	public class SysEvent : Event {
		public string Message { get; }
		public Channel Channel { get; }

		public SysEvent(string message, Channel channel, DateTime? time = null) : base(time) {
			Message = message;
			Channel = channel;
		}
	}

	public class RtbEvent : Event {
		public ServerRtb.Type Type { get; }
		public string Id { get; }
		public Character Character { get; }
		public JObject Payload { get; }

		public RtbEvent(ServerRtb.Type type, string id, Character character, JObject payload, DateTime? time = null) : base(time) {
			Type = type;
			Id = id;
			Character = character;
			Payload = payload;
		}
	}
}