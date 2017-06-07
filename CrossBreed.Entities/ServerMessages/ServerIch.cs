using System.Collections.Generic;

namespace CrossBreed.Entities.ServerMessages {
	public class ServerIch {
		public string channel { get; set; }
		public Channel.ModeEnum mode { get; set; }
		public IReadOnlyCollection<ChannelUser> users { get; set; }
	}
}
