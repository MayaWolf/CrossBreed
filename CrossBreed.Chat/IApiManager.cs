using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace CrossBreed.Chat {
	public interface IApiManager {
		string UserName { get; }
		string Ticket { get; }
		Task<JObject> LogIn(string userName, string password);
		Task<JObject> QueryApi(string path, string parameters = null);
	}
}
