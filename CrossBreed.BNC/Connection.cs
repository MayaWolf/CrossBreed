using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using CrossBreed.Chat;
using CrossBreed.Entities;
using CrossBreed.Entities.ClientMessages;
using CrossBreed.Entities.ServerMessages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace CrossBreed.BNC {
	public class Connection : WebSocketBehavior {
		private User user;
		private static int newClientId = 1;
		private readonly int clientId = newClientId++;
		private readonly Timer pinTimer = new Timer(30000);
		private bool closed, joinComplete;
		private readonly Queue<ServerCommand> sendQueue = new Queue<ServerCommand>();

		public event Action Closed;
		public event Action<ClientCommand> MessageReceived;

		protected override async void OnMessage(MessageEventArgs e) {
			base.OnMessage(e);
			Console.WriteLine($"<<< CLI{clientId}: {e.Data}");
			var raw = e.Data;
			var msg = new ClientCommand(raw.Substring(0, 3).ToEnum<ClientCommandType>(), raw.Length > 3 ? JObject.Parse(raw.Substring(4)) : null);
			if(user == null && msg.Type != ClientCommandType.IDN) {
				Send(Helpers.CreateServerCommand(ServerCommandType.ERR, new ServerErr { number = 3, message = "This command requires that you have logged in." }));
				return;
			}
			if(msg.Type == ClientCommandType.IDN) {
				if(user != null) {
					Send(Helpers.CreateServerCommand(ServerCommandType.ERR, new ServerErr { number = 11, message = "Already identified." }));
					return;
				}
				var request = msg.Payload.ToObject<ClientIdn>();
				user = UserManager.GetUser(request.account);
				if(user == null || !await user.CheckTicket(request.ticket)) {
					user = null;
					Send(Helpers.CreateServerCommand(ServerCommandType.ERR, new ServerErr { number = 4, message = "Identification failed." }));
					return;
				}
				if(closed) return;
				user.OnClientConnect(request.character, this);
				if(closed) return;
				pinTimer.Elapsed += delegate {
					Send(new ServerCommand(ServerCommandType.PIN, null));
				};
				pinTimer.Start();
				return;
			}
			MessageReceived?.Invoke(msg);
		}

		protected override void OnClose(CloseEventArgs e) {
			closed = true;
			base.OnClose(e);
			Closed?.Invoke();
			pinTimer.Dispose();
		}

		public void SendOrEnqueue(ServerCommand message) {
			if(joinComplete) Send(message);
			else {
				lock(sendQueue) {
					if(joinComplete) Send(message);
					else sendQueue.Enqueue(message);
				}
			}
		}

		public void Send(ServerCommand message) {
			string serialized;
			if(message.Payload != null) {
				var sw = new StringWriter();
				sw.Write(message.Type);
				sw.Write(' ');
				var writer = new JsonTextWriter(sw);
				writer.WriteStartObject();
				writer.WritePropertyName("bncTime");
				writer.WriteValue(message.Time);
				foreach(var property in message.Payload.Properties()) property.WriteTo(writer);
				writer.WriteEndObject();
				serialized = sw.ToString();
			} else serialized = message.Type.ToString();
			Console.WriteLine($">>> CLI{clientId}: {serialized}");
			Send(serialized);
		}

		public void SetJoinComplete() {
			lock(sendQueue) {
				joinComplete = true;
				foreach(var message in sendQueue) Send(message);
				sendQueue.Clear();
			}
		}
	}
}