namespace CrossBreed.ViewModels {
	public abstract class ConversationSettings {
		public string Id { get; set; }
		public NotifySettings NotifyMessage { get; set; }
	}

	public class PrivateConversationSettings : ConversationSettings {
		public bool ShowIcon { get; set; }
	}

	public class ChannelConversationSettings : ConversationSettings {
		public NotifySettings NotifyAd { get; set; }
		public NotifySettings NotifyUser { get; set; }
	}

	public class NotifySettings {
		public bool Log { get; set; }
		public bool Notify { get; set; }
		public bool Sound { get; set; }
		public bool Toast { get; set; }
		public bool Flash { get; set; }
	}
}
