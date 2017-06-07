using System;
using System.Collections.Generic;
using CrossBreed.Entities;
using Newtonsoft.Json.Linq;

namespace CrossBreed.Chat {
	public interface IChatManager : IDisposable {
		string OwnCharacterName { get; }
		IDictionary<string, JToken> ServerVars { get; }

		event Action<ServerCommand> CommandReceived;
		event Action Disconnected;
		event Action Connected;

		void Connect(string character, string host);
		void Send(ClientCommand command);
		void SetStatus(StatusEnum status, string message);
	}
}
