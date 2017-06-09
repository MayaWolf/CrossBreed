using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;
using CrossBreed.Chat;
using CrossBreed.Entities;
using CrossBreed.Entities.ClientMessages;
using CrossBreed.Entities.ServerMessages;
using Timer = System.Timers.Timer;

namespace CrossBreed.BNC {
	public class ServerConnection: IDisposable {
		private readonly ICollection<ServerCommand> introMessages = new List<ServerCommand>();
		private readonly ConcurrentDictionary<Connection, Connection> connections = new ConcurrentDictionary<Connection, Connection>();
		private ClientSta customStatus;
		private static readonly ClientSta disconnectedStatus;
		private readonly Timer statusTimer = new Timer(5000);
		private DateTime? lastStatusTime;
		private ClientSta newStatus;
		private static readonly TimeSpan statusInterval = TimeSpan.FromSeconds(5);
		private readonly ManualResetEvent connectComplete = new ManualResetEvent(false);
		private bool disposed;
		private readonly IChatManager chatManager;
		private readonly IBufferManager buffer;
		private readonly IMessageManager messageManager;
		private readonly CharacterManager characterManager;
		private readonly ChannelManager channelManager;
		private readonly ManualResetEvent clientConnectingHandle = new ManualResetEvent(true);

		static ServerConnection() {
			var status = ConfigurationManager.AppSettings["DisconnectedStatus"];
			if(!string.IsNullOrEmpty(status)) {
				var parts = status.Split(';');
				disconnectedStatus = new ClientSta { status = parts[0].ToEnum<StatusEnum>(), statusmsg = parts.Length > 1 ? parts[1] : "" };
			}
		}

		public ServerConnection(IChatManager chatManager, IBufferManager buffer, IMessageManager messageManager, CharacterManager characterManager, ChannelManager channelManager) {
			this.chatManager = chatManager;
			this.buffer = buffer;
			this.messageManager = messageManager;
			this.characterManager = characterManager;
			this.channelManager = channelManager;
			statusTimer.Elapsed += delegate {
				lock(statusTimer) {
					Send(ClientCommandType.STA, newStatus);
					newStatus = null;
					lastStatusTime = DateTime.Now;
				}
			};
			chatManager.CommandReceived += OnServerCommandReceived;
			chatManager.Connected += OnConnectComplete;
		}

		private void SetStatus(ClientSta status) {
			lock(statusTimer) {
				if(newStatus != null) {
					newStatus = status;
					return;
				}
				var now = DateTime.Now;
				if(lastStatusTime != null && now - lastStatusTime < statusInterval) {
					newStatus = status;
					statusTimer.Start();
					statusTimer.AutoReset = false;
				} else {
					Send(ClientCommandType.STA, status);
					lastStatusTime = DateTime.Now;
				}
			}
		}

		private void Send(ClientCommand command) {
			Console.WriteLine(">>> SRV: " + command.Serialize());
			chatManager.Send(command);
		}

		private void Send(ClientCommandType type, object payload) {
			Send(Helpers.CreateClientCommand(type, payload));
		}

		private void OnServerCommandReceived(ServerCommand command) {
			Console.WriteLine("<<< SRV: " + command.Serialize());
			switch(command.Type) {
				case ServerCommandType.HLO:
				case ServerCommandType.VAR:
					introMessages.Add(command);
					return;
			}
			foreach(var connection in connections.Values) connection.SendOrEnqueue(command);
			clientConnectingHandle.WaitOne();
		}

		private void OnConnectComplete() {
			if(disconnectedStatus != null) SetStatus(disconnectedStatus);
			connectComplete.Set();
		}

		private void OnClientMessage(ClientCommand command) {
			switch(command.Type) {
				case ClientCommandType.PIN:
					return;
				case ClientCommandType.PRI:
					messageManager.SendMessage(new Character(command.Payload.Value<string>("recipient")), command.Payload.Value<string>("message"));
					buffer.ClearBuffer();
					return;
				case ClientCommandType.MSG:
				case ClientCommandType.LRP:
					var id = command.Payload.Value<string>("channel");
					var name = id.StartsWith("ADH-", StringComparison.InvariantCultureIgnoreCase) ? channelManager.PrivateChannels[id].Name : id;
					switch(command.Type) {
						case ClientCommandType.MSG:
							messageManager.SendMessage(new Channel(id, name, null), command.Payload.Value<string>("message"));
							break;
						case ClientCommandType.LRP:
							messageManager.SendAd(new Channel(id, name, null), command.Payload.Value<string>("message"));
							break;
					}
					buffer.ClearBuffer();
					return;
				case ClientCommandType.STA:
					customStatus = command.Payload.ToObject<ClientSta>();
					SetStatus(customStatus);
					return;
			}
			Send(command);
		}

		public void AddConnection(Connection connection) {
			var character = characterManager.OwnCharacter;
			connectComplete.WaitOne();
			clientConnectingHandle.Reset();

			connection.Closed += delegate { OnClientDisconnect(connection); };
			connection.MessageReceived += OnClientMessage;

			connection.Send(Helpers.CreateServerCommand(ServerCommandType.IDN, new ServerIdn { character = character.Name }));
			foreach(var message in introMessages) connection.Send(message);
			if(disconnectedStatus != null && connections.Count == 0) SetStatus(customStatus ?? new ClientSta { status = StatusEnum.Online });

			foreach(var message in characterManager.GetJoinCommands()) connection.Send(message);

			connection.Send(Helpers.CreateServerCommand(ServerCommandType.NLN,
				new ServerNln { identity = character.Name, status = character.Status, gender = character.Gender }));

			foreach(var message in channelManager.GetJoinCommands()) connection.Send(message);

			foreach(var message in buffer.GetBuffer()) connection.Send(message);
			connections.TryAdd(connection, connection);
			connection.SetJoinComplete();
			clientConnectingHandle.Set();
		}

		public void Dispose() {
			if(disposed) return;
			disposed = true;
			foreach(var connection in connections.Values) connection.Context.WebSocket.Close();
			statusTimer.Dispose();
			connectComplete.Dispose();
			chatManager.Dispose();
		}

		private void OnClientDisconnect(Connection connection) {
			if(disposed) return;
			connections.TryRemove(connection, out Connection _);
			if(connections.Count == 0 && disconnectedStatus != null) SetStatus(disconnectedStatus);
		}
	}
}