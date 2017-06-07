using System;
using Newtonsoft.Json.Linq;

namespace CrossBreed.Entities {
	public abstract class Command {
		public string Type { get; }
		public DateTime Time { get; }
		public JObject Payload { get; }

		internal Command(string type, JObject payload, DateTime? time = null) {
			Type = type;
			Payload = payload;
			Time = time ?? DateTime.Now;
		}
	}

	public abstract class Command<TMessageType> : Command where TMessageType : struct {
		public new TMessageType Type { get; }

		internal Command(TMessageType messageType, JObject payload, DateTime? time = null) : base(messageType.ToString(), payload, time) {
			Type = messageType;
		}
	}

	public class ClientCommand : Command<ClientCommandType> {
		public ClientCommand(ClientCommandType messageType, JObject payload = null) : base(messageType, payload) {}
	}

	public class ServerCommand : Command<ServerCommandType> {
		public ServerCommand(ServerCommandType messageType, JObject payload, DateTime? time = null) : base(messageType, payload, time) {}
	}
}