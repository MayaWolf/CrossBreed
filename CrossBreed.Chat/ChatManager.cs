using System;
using System.Collections.Generic;
using System.Diagnostics;
using CrossBreed.Entities;
using CrossBreed.Entities.ClientMessages;
using CrossBreed.Entities.ServerMessages;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Websockets;

namespace CrossBreed.Chat {
	public class ChatManager : IChatManager {
		private readonly IApiManager apiManager;
		private readonly IWebSocketConnection socket;
		private bool disposed;
		public string OwnCharacterName { get; private set; }
		public IDictionary<string, JToken> ServerVars { get; } = new Dictionary<string, JToken>();
		public event Action<ServerCommand> CommandReceived;

		public event Action Disconnected {
			add => socket.OnClosed += value;
		    remove => socket.OnClosed -= value;
		}

		public event Action Connected;

		static ChatManager() {
			JsonConvert.DefaultSettings = () => new JsonSerializerSettings {
				Converters = new JsonConverter[] { new StringEnumConverter() },
				NullValueHandling = NullValueHandling.Ignore
			};
		}

		public ChatManager(IApiManager apiManager) {
			this.apiManager = apiManager;
			socket = WebSocketFactory.Create();
			socket.OnMessage += OnServerMessageReceived;
		}

		public async void Connect(string character, string host) {
			OwnCharacterName = character;
			socket.OnOpened += () => Send(Helpers.CreateClientCommand(ClientCommandType.IDN,
				new ClientIdn { account = apiManager.UserName, ticket = apiManager.Ticket, character = OwnCharacterName }));
			socket.Open(host);
		}

		public void Send(ClientCommand msg) {
			//Debug.WriteLine(">>> " + msg.Serialize());
			socket.Send(msg.Serialize());
		}

		public void SetStatus(StatusEnum status, string message) {
			Send(Helpers.CreateClientCommand(ClientCommandType.STA, new ClientSta { status = status, statusmsg = message }));
		}

		private void OnServerMessageReceived(string message) {
			//Debug.WriteLine("<<< " + message);
			var json = message.Length > 4 ? JObject.Parse(message.Substring(4)) : null;
			var msg = new ServerCommand(message.Substring(0, 3).ToEnum<ServerCommandType>(), json, json?.Value<DateTime?>("bncTime") ?? DateTime.Now);
			switch(msg.Type) {
				case ServerCommandType.IDN:
					return;
				case ServerCommandType.VAR:
					ServerVars.Add(msg.Value<string>("variable"), msg.Payload["value"]);
					break;
				case ServerCommandType.PIN:
					Send(new ClientCommand(ClientCommandType.PIN));
					return;
				case ServerCommandType.CON:
					return;
				case ServerCommandType.NLN:
					var nln = msg.Payload.ToObject<ServerNln>();
					if(nln.identity == OwnCharacterName) Connected?.Invoke();
					break;
				case ServerCommandType.ERR:
					var code = msg.Value<int>("number");
					if(code == 2 || code == 3 || code == 4 || code == 6 || code == -4) socket.Close();
					break;
			}
			CommandReceived?.Invoke(msg);
		}

		public void Dispose() {
			if(disposed) return;
			disposed = true;
			if(socket.IsOpen) socket.Close();
		}
	}
}