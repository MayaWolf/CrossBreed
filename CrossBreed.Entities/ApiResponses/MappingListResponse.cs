using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CrossBreed.Entities.ApiResponses {
	public class MappingListResponse {
		[JsonConverter(typeof(ArrayDictionaryConverter), "id")]
		public IReadOnlyDictionary<int, Kink> kinks { get; set; }

		[JsonConverter(typeof(ArrayDictionaryConverter), "id")]
		public IReadOnlyDictionary<int, string> kink_groups { get; set; }

		[JsonConverter(typeof(ArrayDictionaryConverter), "id")]
		public IReadOnlyDictionary<int, InfoTag> infotags { get; set; }

		[JsonConverter(typeof(ArrayDictionaryConverter), "id")]
		public IReadOnlyDictionary<int, string> infotag_groups { get; set; }

		[JsonConverter(typeof(ArrayDictionaryConverter), "id")]
		public IReadOnlyDictionary<int, ListItem> listitems { get; set; }

		public class Kink {
			public string name { get; set; }
			public string description { get; set; }
			public int group_id { get; set; }
		}

		public class InfoTag {
			public string name { get; set; }
			public InfoTagType type { get; set; }
			public string list { get; set; }
			public int group_id { get; set; }
		}

		public class ListItem {
			public string name { get; set; }
			public string value { get; set; }
		}

		public enum InfoTagType {
			text,
			list
		}
	}

	public class ArrayDictionaryConverter : JsonConverter {
		private readonly string key;

		public ArrayDictionaryConverter(string key) {
			this.key = key;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			throw new NotImplementedException();
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			var array = JToken.Load(reader);
			var types = objectType.GenericTypeArguments;
			var dictionary = (IDictionary) Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(types));
			foreach(var item in array.Children<JObject>()) {
				dictionary.Add(item[key].ToObject(types[0]), item.Count > 2 ? item.ToObject(types[1]) : item.Properties().First(x => x.Name != key).Value.ToObject(types[1]));
			}
			return dictionary;
		}

		public override bool CanConvert(Type objectType) {
			throw new NotImplementedException();
		}
	}
}