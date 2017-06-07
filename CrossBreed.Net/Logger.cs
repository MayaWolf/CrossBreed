using System;
using System.Globalization;
using System.IO;
using CrossBreed.Chat;
using CrossBreed.Entities;

namespace CrossBreed.Net {
	public class Logger {
		private string logDirectory;

		public Logger(IChatManager chatManager, IMessageManager messageManager) { //TODO log events
			var settings = AppSettings.Instance.Logging;
			chatManager.Connected += () => {
				logDirectory = Path.Combine(settings.LogDirectory, chatManager.OwnCharacterName);
				Directory.CreateDirectory(logDirectory);
			};
			messageManager.CharacterMessageReceived += (character, message) => {
				if(settings.LogPrivate) LogMessage(character.Name, message);
			};
			messageManager.ChannelMessageReceived += (channel, message) => {
				var isAd = message.MessageType == Message.Type.Ad;
				if(isAd && settings.LogAds || !isAd && settings.LogChannels) LogMessage($"#{channel.Name} - {channel.Id}", message);
			};
		}

		private void LogMessage(string id, Message message) {
			var fileName = DateTime.Now.ToString("yyy-MM-dd") + (message.MessageType == Message.Type.Message ? "-Ads" : "") + ".txt";
			var dir = Path.Combine(logDirectory, id);
			Directory.CreateDirectory(dir);
			using(var writer = new StreamWriter(Path.Combine(dir, fileName), true)) {
				var name = $"[user]{message.Sender.Name}[/user]";
				var text = message.Text.StartsWith("/me") ? $"*{name}{message.Text.Substring(3)}" : $"{name}: {message.Text}";
				writer.WriteLine($"[{message.Time.ToString("t", CultureInfo.CurrentUICulture)}] {text}");
			}
		}
	}
}
