using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CrossBreed.Chat;
using CrossBreed.Entities;
using MvvmCross.Plugins.File;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CrossBreed.ViewModels {
	public class LogManager : ILogManager {
		private readonly IMvxFileStore fileManager;
		private string logDirectory;
		private readonly string eventsLogFile;
		private readonly MessageSerializer serializer;
		private readonly JsonSerializerSettings eventSerializerSettings;
		private readonly IDictionary<string, Dictionary<DateTime, long>> index = new Dictionary<string, Dictionary<DateTime, long>>();

		public LogManager(IChatManager chatManager, ICharacterManager characterManager, IMessageManager messageManager, IEventManager eventManager, IMvxFileStore fileManager) {
			this.fileManager = fileManager;
			var settings = AppSettings.Instance.Logging;
			serializer = new MessageSerializer(characterManager);
			eventSerializerSettings = new JsonSerializerSettings {
				Converters = new JsonConverter[] { new CharacterConverter(characterManager), new ChannelMemberConverter(characterManager), new ChannelConverter() },
				TypeNameHandling = TypeNameHandling.Auto,
				SerializationBinder = new EventSerializationBinder()
			};
			if(settings.LogDirectory == null) return;
			eventsLogFile = Path.Combine(settings.LogDirectory, "EventLog");
			chatManager.Connected += () => {
				logDirectory = Path.Combine(settings.LogDirectory, chatManager.OwnCharacterName);
				fileManager.EnsureFolderExists(logDirectory);
			};
			messageManager.CharacterMessageReceived += (character, message) => {
				if(settings.LogPrivate) LogMessage(GetLogFile(GetLogId(character), false), message);
			};
			messageManager.ChannelMessageReceived += (channel, message) => {
				var isAd = message.MessageType == Message.Type.Ad;
				if(isAd && settings.LogAds || !isAd && settings.LogChannels) LogMessage(GetLogFile(GetLogId(channel), isAd), message);
			};
			eventManager.NewEvent += NewEvent;
		}

		private void NewEvent(Event e) {
			var stream = fileManager.OpenWrite(eventsLogFile);
			stream.Seek(0, SeekOrigin.End);
			using(var writer = new StreamWriter(stream)) {
				writer.WriteLine(JsonConvert.SerializeObject(e, typeof(Event), Formatting.None, eventSerializerSettings));
			}
		}

		public string GetLogId(Channel channel) => $"#{channel.Id}";

		public string GetLogId(Character character) => character.Name;

		public IEnumerable<string> GetAllLogIds() => fileManager.GetFoldersIn(logDirectory).Select(Path.GetFileName);

		public IEnumerable<DateTime> GetDays(string id, bool ads) => GetIndex(GetLogFile(id, ads)).Keys;

		private Dictionary<DateTime, long> GetIndex(string key) {
			if(index.ContainsKey(key)) return index[key];
			using(var stream = fileManager.OpenRead(key + ".idx")) {
				if(stream == null) return index[key] = new Dictionary<DateTime, long>();
				return index[key] = serializer.ReadIndex(stream);
			}
		}

		public IReadOnlyList<Message> LoadDay(string id, bool ads, DateTime day) {
			var file = GetLogFile(id, ads);
			if(!fileManager.Exists(file)) return new List<Message>();
			using(var stream = fileManager.OpenRead(file)) {
				stream.Position = index[file][day];
				return serializer.Load(stream).TakeWhile(x => x.Time.Date == day).ToList();
			}
		}

		public IReadOnlyList<Message> Load(string id, bool ads, int count) {
			var pos = -1L;
			return Load(id, ads, count, ref pos);
		}

		public IReadOnlyList<Message> Load(string id, bool ads, int count, ref long position) {
			var file = GetLogFile(id, ads);
			if(!fileManager.Exists(file)) return new List<Message>();
			using(var stream = fileManager.OpenRead(file)) {
				stream.Position = position;
				var messages = serializer.Load(stream).Take(count).ToList();
				position = stream.Position;
				return messages;
			}
		}

		public IReadOnlyList<Message> LoadReverse(string id, bool ads, int count) {
			var pos = -1L;
			return LoadReverse(id, ads, count, ref pos);
		}

		public IReadOnlyList<Message> LoadReverse(string id, bool ads, int count, ref long position) {
			var file = GetLogFile(id, ads);
			if(!fileManager.Exists(file)) return new List<Message>();
			using(var stream = fileManager.OpenRead(file)) {
				if(position == -1) position = stream.Length;
				stream.Position = position;
				var messages = serializer.LoadReverse(stream, count);
				position = stream.Position;
				return messages;
			}
		}

		private string GetLogFile(string id, bool ads) {
			var dir = Path.Combine(logDirectory, id);
			fileManager.EnsureFolderExists(dir);
			return Path.Combine(dir, ads ? "Ads" : "Logs");
		}

		private void LogMessage(string file, Message message) {
			var index = GetIndex(file);
			var date = message.Time.Date;
			using(var stream = fileManager.OpenWrite(file)) {
				stream.Seek(0, SeekOrigin.End);

				if(!index.ContainsKey(date)) {
					var pos = stream.Position;
					index.Add(date, pos);
					using(var indexStream = fileManager.OpenWrite(file + ".idx")) {
						indexStream.Position = indexStream.Length;
						serializer.WriteIndex(indexStream, date, pos);
					}
				}

				serializer.Write(stream, message);
			}
		}

		public class CharacterConverter : JsonConverter {
			private readonly ICharacterManager characterManager;

			public CharacterConverter(ICharacterManager characterManager) {
				this.characterManager = characterManager;
			}

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
				writer.WriteValue(((Character) value).Name);
			}

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
				return characterManager.GetCharacter((string) reader.Value);
			}

			public override bool CanConvert(Type objectType) => objectType == typeof(Character);
		}

		public class ChannelMemberConverter : JsonConverter {
			private readonly ICharacterManager characterManager;

			public ChannelMemberConverter(ICharacterManager characterManager) {
				this.characterManager = characterManager;
			}

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
				writer.WriteValue(((Channel.Member) value).Character.Name);
			}

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
				return new Channel.Member(characterManager.GetCharacter((string) reader.Value));
			}

			public override bool CanConvert(Type objectType) => objectType == typeof(Channel.Member);
		}

		public class ChannelConverter : JsonConverter {
			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
				var channel = (Channel) value;
				writer.WriteStartObject();
				writer.WritePropertyName("Id");
				writer.WriteValue(channel.Id);
				writer.WritePropertyName("Name");
				writer.WriteValue(channel.Name);
				writer.WriteEndObject();
			}

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
				reader.Read();
				reader.Read();
				var id = reader.ReadAsString();
				reader.Read();
				var name = reader.ReadAsString();
				reader.Read();
				return new Channel(id, name, null);
			}

			public override bool CanConvert(Type objectType) => objectType == typeof(Channel);
		}

		public class EventSerializationBinder : DefaultSerializationBinder {
			public override void BindToName(Type serializedType, out string assemblyName, out string typeName) {
				base.BindToName(serializedType, out string _, out typeName);
				assemblyName = null;
			}
		}
	}
}