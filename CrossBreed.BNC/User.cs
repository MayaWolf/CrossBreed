using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CrossBreed.Chat;
using CrossBreed.Entities;
using CrossBreed.Entities.ServerMessages;
using CrossBreed.Net;
using Newtonsoft.Json.Linq;

namespace CrossBreed.BNC {
	public class User : IDisposable {
		public string Name { get; }
		private readonly string password;
		private readonly IDictionary<string, ServerConnection> connections = new Dictionary<string, ServerConnection>();
		private readonly IDictionary<string, BufferManager> buffers = new Dictionary<string, BufferManager>();
		private readonly IApiManager apiManager;

		public User(string name, string password) {
			Name = name;
			this.password = password;
			apiManager = new ApiManager();
		}

		private async Task Connect(string character) {
			var response = await apiManager.LogIn(Name, password);
			if(response.TryGetValue("error", out JToken error) && !string.IsNullOrEmpty((string) error)) {
				Console.WriteLine("ERR: FList authentication failed - " + error);
				Thread.Sleep(50000);
				OnConnectionLoss(character);
				return;
			}
			if(!buffers.ContainsKey(character)) buffers.Add(character, new BufferManager());
			var bufferManager = buffers[character];

			var chatManager = new ChatManager(apiManager, typeof(User).Assembly.GetName());
			bufferManager.SetEventSource(chatManager);
			var characterManager = new CharacterManager(chatManager, apiManager);
			var channelManager = new ChannelManager(chatManager, characterManager);
			var messageManager = new MessageManager(chatManager, characterManager, channelManager);
			var connection = new ServerConnection(chatManager, bufferManager, messageManager, characterManager, channelManager);
			var logManager = new Logger(chatManager, messageManager);

			chatManager.Disconnected += () => OnConnectionLoss(character);
			connections.Add(character, connection);
			chatManager.Connect(character, ConfigurationManager.AppSettings["Host"]);
		}

		public async void Connect(IEnumerable<string> characters) {
			foreach(var character in characters) await Connect(character);
		}

		private void OnConnectionLoss(string character) {
			if(connections.ContainsKey(character)) {
				connections[character].Dispose();
				connections.Remove(character);
			}
			Thread.Sleep(10000);
			Connect(character);
		}

		public async Task<bool> CheckTicket(string ticket) {
			try {
				using(var client = new HttpClient()) {
					var requestContent = new StringContent($"account={HttpUtility.UrlEncode(Name)}&ticket={ticket}", Encoding.UTF8, "application/x-www-form-urlencoded");
					var response = await client.PostAsync("https://www.f-list.net/json/api/auth.php", requestContent);
					var responseContent = await response.Content.ReadAsStringAsync();
					return responseContent == "";
				}
			} catch(HttpRequestException) {
				return false;
			}
		}

		public void OnClientConnect(string character, Connection connection) {
			if(!connections.ContainsKey(character)) {
				connection.Send(Helpers.CreateServerCommand(ServerCommandType.ERR, new ServerErr { number = 6, message = "The character requested was not found." }));
				return;
			}
			connections[character].AddConnection(connection);
		}

		public void Dispose() {
			foreach(var connection in connections) connection.Value.Dispose();
		}
	}
}