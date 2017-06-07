using System.Collections.Generic;
using System.Configuration;

namespace CrossBreed.BNC {
	public static class UserManager {
		private static readonly IDictionary<string, User> users = new Dictionary<string, User>();

		public static void Init() {
			var name = ConfigurationManager.AppSettings["UserName"];
			if(name == null) throw new ConfigurationErrorsException("No user configured!");
			var characters = ConfigurationManager.AppSettings["Characters"]?.Split(';');
			if(characters == null) throw new ConfigurationErrorsException("No characters configured!");
			var user = new User(name, ConfigurationManager.AppSettings["Password"]);
			users.Add(name, user);
			user.Connect(characters);
		}

		public static User GetUser(string name) {
			users.TryGetValue(name, out User user);
			return user;
		}
	}
}
