using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CrossBreed.Entities.ServerMessages {
	public class ServerLis {
		[JsonConverter(typeof(Converter))]
		public ICollection<Character> characters { get; set; }

		private class Converter : JsonConverter {
			private readonly StringEnumConverter enumConverter = new StringEnumConverter();

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
				var characters = (IEnumerable<Character>) value;
				writer.WriteStartArray();
				foreach(var character in characters) {
					writer.WriteStartArray();
					writer.WriteValue(character.Name);
					enumConverter.WriteJson(writer, character.Gender, serializer);
					enumConverter.WriteJson(writer, character.Status, serializer);
					writer.WriteValue(character.StatusMessage);
					writer.WriteEndArray();
				}
				writer.WriteEndArray();
			}

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
				var list = new List<Character>(100);
				reader.Read();
				while(true) {
					var character = new Character(reader.ReadAsString());
					reader.Read();
					character.Gender = (GenderEnum) enumConverter.ReadJson(reader, typeof(GenderEnum), null, serializer);
					reader.Read();
					character.Status = (StatusEnum) enumConverter.ReadJson(reader, typeof(StatusEnum), null, serializer);
					character.StatusMessage = reader.ReadAsString();
					list.Add(character);
					reader.Read();
					reader.Read();
					if(reader.TokenType == JsonToken.EndArray) break;
				}
				return list;
			}

			public override bool CanConvert(Type objectType) => objectType == typeof(Character);
		}
	}
}