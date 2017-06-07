using System;
using System.Collections.Generic;
using System.Linq;
using CrossBreed.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CrossBreed.Chat {
	public static class Helpers {
		public static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

		public static string Serialize(this Command command) =>
			command.Type + (command.Payload == null ? "" : " " + command.Payload.ToString(Formatting.None));

		public static TEnum ToEnum<TEnum>(this string str) where TEnum : struct => (TEnum) Enum.Parse(typeof(TEnum), str, true);

		public static ClientCommand CreateClientCommand(ClientCommandType type, object payload) => new ClientCommand(type, JObject.FromObject(payload));

		public static ServerCommand CreateServerCommand(ServerCommandType type, object payload) => new ServerCommand(type, JObject.FromObject(payload));

		public static T Value<T>(this Command command, string key) => command.Payload.Value<T>(key);

		public static DateTime UnixToDateTime(int unix) => UnixEpoch.AddSeconds(unix);

		public static GenderEnum GetGender(string genderName) {
			GenderEnum gender;
			switch(genderName) {
				case "Cunt-boy":
					gender = GenderEnum.Cuntboy;
					break;
				case "Male-Herm":
					gender = GenderEnum.Maleherm;
					break;
				default:
					gender = genderName.ToEnum<GenderEnum>();
					break;
			}
			return gender;
		}
	}
}