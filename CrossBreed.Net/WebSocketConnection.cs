using System;
using Websockets;
using WebSocketSharp;

namespace CrossBreed.Net {
	public class WebSocketConnection : IWebSocketConnection {
		private WebSocket socket;
		public bool IsOpen => socket.IsAlive;
		public event Action OnOpened;
		public event Action OnClosed;
		public event Action<IWebSocketConnection> OnDispose;
		public event Action<string> OnError;
		public event Action<string> OnMessage;
		public event Action<string> OnLog;

		public void Open(string url, string protocol = null) {
			socket = protocol == null ? new WebSocket(url) : new WebSocket(url, protocol);
			socket.OnOpen += delegate { OnOpened?.Invoke(); };
			socket.OnClose += delegate { OnClosed?.Invoke(); };
			socket.OnError += (sender, args) => OnError?.Invoke(args.Message);
			socket.OnMessage += (sender, args) => OnMessage?.Invoke(args.Data);
			socket.Connect();
		}

		public void Close() {
			socket.Close();
		}

		public void Send(string message) {
			socket.Send(message);
		}

		public void Dispose() {
			if(socket.IsAlive) socket.Close();
			OnDispose?.Invoke(this);
		}
	}
}