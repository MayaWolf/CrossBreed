using System;
using System.Collections.Generic;
using CrossBreed.Entities;
using ML.Settings;

namespace CrossBreed.Chat {
	public class AppSettings : BaseSettings {
		public static AppSettings Instance { get; }
		public IReadOnlyCollection<string> PinnedChannels { get; set; } = new[] { "ADH-51d8622fe6fcff7c5c6c" }; //TODO

		public LoggingSettings Logging { get; }

		public class LoggingSettings: BaseSettings {
			public bool LogPrivate { get; set; } = true;
			public bool LogChannels { get; set; }
			public bool LogAds { get; set; }
			public string LogDirectory { get; set; }
		}
	}
}