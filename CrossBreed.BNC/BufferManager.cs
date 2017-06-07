using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using CrossBreed.Chat;
using CrossBreed.Entities;

namespace CrossBreed.BNC {
	public class BufferManager : IBufferManager {
		private readonly ConcurrentQueue<ServerCommand> buffer = new ConcurrentQueue<ServerCommand>();
		private static readonly bool shouldBufferPrivate = bool.Parse(ConfigurationManager.AppSettings["BufferPrivate"]);
		private static readonly bool shouldBufferChannel = bool.Parse(ConfigurationManager.AppSettings["BufferChannels"]);

		public void SetEventSource(IChatManager events) {
			events.CommandReceived += OnCommandReceived;
		}

		private void OnCommandReceived(ServerCommand msg) {
			switch(msg.Type) {
				case ServerCommandType.MSG:
					if(!shouldBufferChannel) return;
					break;
				case ServerCommandType.RLL:
					if(msg.Payload["channel"] == null) {
						if(!shouldBufferPrivate) return;
					} else if(!shouldBufferChannel) return;
					return;
				case ServerCommandType.PRI:
					if(!shouldBufferPrivate) return;
					break;
				default:
					return;
			}
			buffer.Enqueue(new ServerCommand(msg.Type, msg.Payload, DateTime.UtcNow));
		}

		public IEnumerable<ServerCommand> GetBuffer() => buffer;

		public void ClearBuffer() {
			while(buffer.TryDequeue(out ServerCommand _)) { }
		}
	}
}