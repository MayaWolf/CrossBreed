using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace CrossBreed.Chat {
	public class ApiManager : IApiManager {
		private const string BaseApiPath = "https://www.f-list.net/json/api/";
		public string UserName { get; private set; }
		private string password;
		public string Ticket { get; private set; }

		private async Task<JObject> TryGetTicket() {
			using(var client = new HttpClient()) {
				var response = await client.PostAsync("https://www.f-list.net/json/getApiTicket.php",
					new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("account", UserName), new KeyValuePair<string, string>("password", password) }));
				var responseContent = await response.Content.ReadAsStringAsync();
				var obj = JObject.Parse(responseContent);
				Ticket = obj.Value<string>("ticket");
				return obj;
			}
		}

		public Task<JObject> LogIn(string userName, string password) {
			UserName = userName;
			this.password = password;
			return TryGetTicket();
		}

		/*public async Task<string> GetTicket(bool createNew = false) {
			if(ticket != null && !createNew) return ticket;
			return ticket = (await TryGetTicket())?.Value<string>("ticket");
		}*/

		private async Task<JObject> DoQueryApi(string path, string parameters) {
			using(var client = new HttpClient()) {
				var request = new StringContent($"account={UserName}&ticket={Ticket}" + (string.IsNullOrEmpty(parameters) ? "" : $"&{parameters}"),
					Encoding.UTF8, "application/x-www-form-urlencoded");
				var response = await client.PostAsync(BaseApiPath + path, request);
				return JObject.Parse(await response.Content.ReadAsStringAsync());
			}
		}

		public async Task<JObject> QueryApi(string path, string parameters = null) {
			var json = await DoQueryApi(path, parameters);
			if(json.Value<string>("error") == "Invalid ticket.") {
				var res = await TryGetTicket();
				if(Ticket == null) return res;
				json = await QueryApi(path, parameters);
			}
			return json;
		}
	}
}