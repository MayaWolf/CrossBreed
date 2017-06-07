using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CrossBreed.Entities.ApiResponses {
	public class ProfileResponse {
		public string description { get; set; }
		public int views { get; set; }
		public bool customs_first { get; set; }
		public string custom_title { get; set; }
		public int created_at { get; set; }
		public int updated_at { get; set; }

		[JsonConverter(typeof(WorkaroundConverter))]
		public IReadOnlyDictionary<int, KinkChoiceEnum> kinks { get; set; }

		[JsonConverter(typeof(WorkaroundConverter))]
		public IReadOnlyDictionary<int, CustomKink> custom_kinks { get; set; }

		[JsonConverter(typeof(WorkaroundConverter))]
		public IReadOnlyDictionary<int, string> infotags { get; set; }

		public IReadOnlyCollection<Image> images { get; set; }

		[JsonConverter(typeof(WorkaroundConverter))]
		public IReadOnlyDictionary<int, Inline> inlines { get; set; }


		public class CustomKink {
			public string name { get; set; }
			public string description { get; set; }
			public KinkChoiceEnum choice { get; set; }
			public IReadOnlyCollection<int> children { get; set; }
		}

		public class Image {
			public int image_id { get; set; }
			public string extension { get; set; }
			public int height { get; set; }
			public int width { get; set; }
			public string description { get; set; }
			public int sort_order { get; set; }
		}

		public class Inline {
			public string name { get; set; }
			public string hash { get; set; }
			public string extension { get; set; }
			public bool nsfw { get; set; }
		}

		public class WorkaroundConverter : JsonConverter {
			//TODO workaround until profile API is fixed
			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
				throw new NotImplementedException();
			}

			public override bool CanConvert(Type objectType) => true;

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
				if(reader.TokenType != JsonToken.StartArray) return JObject.Load(reader).ToObject(objectType, serializer);
				JArray.Load(reader);
				return Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(objectType.GenericTypeArguments));
			}
		}
	}
}