using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CrossBreed.Chat;
using CrossBreed.Entities;
using CrossBreed.ViewModels;
using MvvmCross.Platform;
using Newtonsoft.Json;

namespace CrossBreed.Windows {
	public static class SlimCatImporter {
		private static readonly string slimCatRoaming = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "slimCat");
		private static readonly string slimCatLocal = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "slimCat");

		public static bool CanImportRoaming() => Directory.Exists(slimCatRoaming);
		public static bool CanImportLocal() => Directory.Exists(slimCatLocal);

		public static void ImportLocal() {
			var clientSettings = ClientSettings.Instance;
			if(!Directory.Exists(slimCatLocal)) return;
			var configFile = Directory.EnumerateFiles(slimCatLocal, "*.config", SearchOption.AllDirectories).OrderByDescending(File.GetLastWriteTime).FirstOrDefault();
			if(configFile == null) return;
			var config = XDocument.Parse(File.ReadAllText(configFile)).Root?.Element("userSettings")?.Element("slimCat.Properties.Settings")?.Descendants().ToList();
			if(config == null) return;
			clientSettings.UserName = config.FirstOrDefault(x => (string) x.Attribute("name") == "UserName")?.Element("value")?.Value;
			clientSettings.Password = config.FirstOrDefault(x => (string) x.Attribute("name") == "Password")?.Element("value")?.Value;
			clientSettings.Host = config.FirstOrDefault(x => (string) x.Attribute("name") == "Host")?.Element("value")?.Value;
			clientSettings.SaveLogin = true;

			if(CanImportRoaming()) {
				var provider = Mvx.GetSingleton<CharacterListProvider>();
				var settings = XDocument.Parse(File.ReadAllText(Path.Combine(slimCatRoaming, "!Defaults", "Global", "!settings.xml"))).Root;
				if(settings == null) return;

				TrySetBool(settings.Element("HideFriendsFromSearchResults"), b => provider.Friends.HideInSearch = b);
				var customLists = provider.CustomLists.ToList();
				var interested = settings.Element("Interested");
				if(interested != null) customLists[1].Characters = new HashSet<string>(interested.Elements("character").Select(x => x.Value).Distinct());
				var interestColoring = settings.Element("AllowOfInterestColoring");
				if(interestColoring != null && !bool.Parse(interestColoring.Value)) customLists[1].UnderlineColor = 0;
				var notInterested = settings.Element("NotInterested");
				if(notInterested != null) customLists[1].Characters = new HashSet<string>(notInterested.Elements("character").Select(x => x.Value).Distinct());
				provider.SaveCustomLists(customLists);
			}
		}

		public static void ImportRoaming(string characterName) {
			var appSettings = AppSettings.Instance;
			var userSettings = UserSettings.Instance;
			var characterSettings = CharacterSettings.Instance;

			var characterPath = Path.Combine(slimCatRoaming, characterName, "Global", "!settings.xml");
			var settings = XDocument.Parse(File.ReadAllText(File.Exists(characterPath) ? characterPath : Path.Combine(slimCatRoaming, "!Defaults", "Global", "!settings.xml"))).Root;
			if(settings == null) return;

			var disallowedBb = new List<string>();
			var allowColors = settings.Element("AllowColors");
			if(allowColors != null && !bool.Parse(allowColors.Value)) disallowedBb.Add("color");
			var allowIndent = settings.Element("AllowIndent");
			if(allowIndent != null && !bool.Parse(allowIndent.Value)) disallowedBb.Add("indent");
			var allowIcons = settings.Element("AllowIcons");
			if(allowIcons != null && !bool.Parse(allowIcons.Value)) {
				disallowedBb.Add("icon");
				disallowedBb.Add("eicon");
			}
			var allowAlign = settings.Element("AllowAlignment");
			if(allowAlign != null && !bool.Parse(allowAlign.Value)) {
				disallowedBb.Add("left");
				disallowedBb.Add("center");
				disallowedBb.Add("right");
				disallowedBb.Add("justify");
			}
			userSettings.General.DisallowedBbCode = disallowedBb;

			TrySetBool(settings.Element("AllowLogging"), b => userSettings.Logging.LogPrivate = userSettings.Logging.LogChannels = b);
			TrySetBool(settings.Element("AllowSound"), b => userSettings.Notifications.Sounds = b);
			TrySetBool(settings.Element("AllowMinimizeToTray"), b => userSettings.General.CloseToTray = b);
			TrySetBool(settings.Element("CheckForOwnName"), b => userSettings.Notifications.NotifyHighlights = b);
			TrySet(settings.Element("GlobalNotifyTerms"), x => { userSettings.Notifications.HighlightWords = x.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList(); });
			TrySetBool(settings.Element("OpenProfilesInClient"), b => userSettings.General.UseProfileViewer = b);
			TrySet(settings.Element("FontSize"), x => ThemeSettings.Instance.FontSize = int.Parse(x));
			TrySetBool(settings.Element("PlaySoundEvenWhenTabIsFocused"), b => userSettings.Notifications.AlwaysPlaySound = b);
			TrySetBool(settings.Element("StickNewMessagesToBottom"), b => userSettings.General.MessagesFromBottom = b);

			var recent = settings.Element("RecentCharacters");
			if(recent != null) characterSettings.RecentCharacters = recent.Elements("character").Select(x => x.Value).Distinct().ToList();

			var pinned = settings.Element("SavedChannels");
			if(pinned != null) appSettings.PinnedChannels = appSettings.PinnedChannels.Concat(pinned.Elements("channel").Select(x => x.Value)).Distinct().ToList();
		}

		private static void TrySet(XElement element, Action<string> setter) {
			if(element != null) setter(element.Value);
		}

		private static void TrySetBool(XElement element, Action<bool> setter) {
			bool b;
			if(element != null && bool.TryParse(element.Value, out b)) setter(b);
		}

		private static readonly ISet<string> officialChannels = new HashSet<string> {
			"Canon Characters", "Monster's Lair", "German IC", "Humans/Humanoids", "Warhammer General", "Love and Affection", "Transformation", "Hyper Endowed", "Force/Non-Con", "Diapers/Infantilism",
			"Avians", "Politics", "Lesbians", "Superheroes", "Footplay", "Sadism/Masochism", "German Politics", "Para/Multi-Para RP", "Micro/Macro", "Ferals / Bestiality", "Gamers", "Gay Males",
			"Story Driven LFRP", "Femdom", "German OOC", "World of Warcraft", "Ageplay", "German Furry", "Scat Play", "Hermaphrodites", "RP Dark City", "All in the Family", "Inflation", "Development",
			"Fantasy", "Frontpage", "Pokefurs", "Medical Play", "Domination/Submission", "Latex", "Fat and Pudgy", "Muscle Bound", "Furries", "RP Bar", "The Slob Den", "Artists / Writers", "Mind Control",
			"Ass Play", "Sex Driven LFRP", "Gay Furry Males", "Vore", "Non-Sexual RP", "Equestria ", "Sci-fi", "Watersports", "Straight Roleplay", "Gore", "Cuntboys", "Femboy", "Bondage", "Cum Lovers",
			"Transgender", "Pregnancy and Impregnation", "Canon Characters OOC", "Dragons", "Helpdesk"
		};

		private static readonly Regex logRegex = new Regex(@"^(Ad at \[\d{2}:\d{2}]:|\[\d{2}:\d{2}] (\[user\])?([A-Za-z0-9 \-_]+))");
		private static readonly Regex eventRegex = new Regex(@"^\[\d{2}:\d{2}] ");
		private static readonly Regex characterRegex = new Regex(@"([A-Za-z0-9][A-Za-z0-9 \-_]{0,18}[A-Za-z0-9\-_])\W");

		private static readonly Regex statusRegex =
			new Regex(@"^\[(\d{2}:\d{2})] ([A-Za-z0-9 \-_]+) (is now (\w+)( and has blanked their status|: )?|has updated their status: |has blanked their status)(.*).$", RegexOptions.Singleline);

		private static readonly Regex loginRegex = new Regex(@"^\[(\d{2}:\d{2})] (.*) has logged in.$");
		private static readonly Regex logoutRegex = new Regex(@"^\[(\d{2}:\d{2})] (.*) has logged out.$");
		private static readonly Regex joinRegex = new Regex(@"^\[(\d{2}:\d{2})] (.*) has joined (.*).$");
		private static readonly Regex leaveRegex = new Regex(@"^\[(\d{2}:\d{2})] (.*) has left (.*).$");
		private static readonly Regex descriptionRegex = new Regex(@"^\[(\d{2}:\d{2})] \[session=(.*)\](.*)\[/session\] has a new description.$");
		private static readonly Regex opChangeRegex = new Regex(@"^\[(\d{2}:\d{2})] (.*) has been (demoted from|promoted to) channel moderator in (.*).$");
		private static readonly Regex openRegex = new Regex(@"^\[(\d{2}:\d{2})] \[session=(.*)\](.*)\[/session\] is now (open|closed)$");
		private static readonly Regex listRegex = new Regex(@"^\[(\d{2}:\d{2})] (.*) has been (added to|removed from) your (Bookmark|Friend) list.$");
		private static readonly Regex friendRegex = new Regex(@"^\[(\d{2}:\d{2})] (.*) has (sent you a|received your) friend request$");
		private static readonly Regex kickRegex = new Regex(@"^\[(\d{2}:\d{2})] \[user\](.*)\[/user\] has (kicked|banned) (.*) from \[session=(.*)\](.*)\[/session\]$");
		private static readonly Regex noteRegex = new Regex(@"^\[(\d{2}:\d{2})] (.*) has sent you a note: \[url=.*\](.*)\[/url\]$");
		private static readonly Regex broadcastRegex = new Regex(@"^\[(\d{2}:\d{2})] ([A-Za-z0-9 \-_]+) broadcasted (.*)$");
		private static readonly Regex mentionRegex = new Regex(@"^\[(\d{2}:\d{2})] (.*) mentioned (.*) in (.*): ""(.*)""$");
		private static readonly Regex inviteRegex = new Regex(@"^\[(\d{2}:\d{2})] \[user\](.*)\[/user\] has invited you to join \[session=(.*)\](.*)\[/session\]$");

		public static void ImportLogs(string characterName) {
			var characterPath = Path.Combine(App.DataDirectory, characterName);
			var slimcatPath = Path.Combine(slimCatRoaming, characterName);
			var eventSerializerSettings = new JsonSerializerSettings {
				Converters = new JsonConverter[] { new LogManager.CharacterConverter(null), new LogManager.ChannelMemberConverter(null), new LogManager.ChannelConverter() },
				TypeNameHandling = TypeNameHandling.Auto,
				SerializationBinder = new LogManager.EventSerializationBinder()
			};
			var serializer = new MessageSerializer(null);
			foreach(var dir in Directory.EnumerateDirectories(slimcatPath)) {
				var name = Path.GetFileName(dir);
				var files = Directory.GetFiles(dir, "*.txt").Select(x => {
					try {
						var date = Path.GetFileNameWithoutExtension(x).Split('-').Select(int.Parse).ToArray();
						return new { Name = x, Date = new DateTime(date[2], date[0], date[1]) };
					} catch {
						return null;
					}
				}).Where(x => x != null).OrderBy(x => x.Date);
				if(name == "!Notifications") {
					var events = new StreamWriter(Path.Combine(characterPath, "EventLog"));
					foreach(var file in files) {
						foreach(var line in GetLogEntries(File.ReadLines(file.Name), eventRegex)) {
							try {
								Match match;
								Event e = null;

								DateTime GetTime() {
									var time = match.Groups[1].Value;
									return file.Date.AddMinutes(int.Parse(time.Substring(0, 2)) * 60 + int.Parse(time.Substring(3)));
								}

								if((match = statusRegex.Match(line)).Success) {
									var status = match.Groups[4].Success ? match.Groups[4].Value.ToEnum<StatusEnum>() : StatusEnum.Online;
									e = new StatusEvent(new Character(match.Groups[2].Value), status, match.Groups[6].Value, GetTime());
								} else if((match = loginRegex.Match(line)).Success) {
									e = new LoginEvent(new Character(match.Groups[2].Value), GetTime());
								} else if((match = logoutRegex.Match(line)).Success) {
									e = new LogoutEvent(new Character(match.Groups[2].Value), GetTime());
								} else if((match = joinRegex.Match(line)).Success) {
									e = new ChannelJoinEvent(new Channel(null, match.Groups[3].Value, null), new Channel.Member(new Character(match.Groups[2].Value)), GetTime());
								} else if((match = leaveRegex.Match(line)).Success) {
									e = new ChannelLeaveEvent(new Channel(null, match.Groups[3].Value, null), new Channel.Member(new Character(match.Groups[2].Value)), GetTime());
								} else if((match = descriptionRegex.Match(line)).Success) {
									continue;
								} else if((match = opChangeRegex.Match(line)).Success) {
									continue;
								} else if((match = openRegex.Match(line)).Success) {
									continue;
								} else if((match = listRegex.Match(line)).Success) {
									continue;
								} else if((match = friendRegex.Match(line)).Success) {
									continue;
								} else if((match = kickRegex.Match(line)).Success) {
									continue;
								} else if((match = noteRegex.Match(line)).Success) {
									e = new NoteEvent(new Character(match.Groups[2].Value), null, match.Groups[3].Value, GetTime());
								} else if((match = broadcastRegex.Match(line)).Success) {
									e = new BroadcastEvent(new Character(match.Groups[2].Value), match.Groups[3].Value, GetTime());
								} else if((match = mentionRegex.Match(line)).Success) {
									var time = GetTime();
									e = new MentionEvent(new Channel(null, match.Groups[4].Value, null),
										new Message(Message.Type.Message, new Character(match.Groups[2].Value), time, match.Groups[5].Value), GetTime());
								} else if((match = inviteRegex.Match(line)).Success) {
									e = new InviteEvent(new Character(match.Groups[2].Value), new ChannelListItem(match.Groups[4].Value, match.Groups[3].Value, 0), GetTime());
								}
								if(e != null) events.WriteLine(JsonConvert.SerializeObject(e, typeof(Event), Formatting.None, eventSerializerSettings));
							} catch {}
						}
					}
					events.Dispose();
					continue;
				}
				Stream logs = null, ads = null, logsIndex = null, adsIndex = null;
				var parenIndex = name.IndexOf('(');
				var logDir = Path.Combine(characterPath, parenIndex != -1 ? $"#{name.Substring(parenIndex + 1, name.Length - parenIndex - 2)}" : 
					officialChannels.Contains(name) ? $"#{name}" : name);
				if(Directory.Exists(logDir)) continue;

				Directory.CreateDirectory(logDir);
				foreach(var file in files) {
					var logsIndexed = false;
					var adsIndexed = false;
					foreach(var line in GetLogEntries(File.ReadLines(file.Name), logRegex)) {
						try {
							var index = 0;
							var type = Message.Type.Message;
							string sender;
							if(line.StartsWith("Ad at")) {
								type = Message.Type.Ad;
								index += 6;
							}
							var h = int.Parse(line.Substring(index + 1, 2));
							var m = int.Parse(line.Substring(index + 4, 2));
							index += 8;
							var text = "";
							if(type == Message.Type.Ad) {
								var end = line.LastIndexOf("~By ");
								if(end != -1) {
									text = line.Substring(index, end - index);
									sender = line.Substring(end + 4);
								} else {
									sender = characterRegex.Match(line, index, Math.Min(20, line.Length - index)).Groups[1].Value;
									index += sender.Length;
									text = index < line.Length ? line.Substring(index) : "";
								}
							} else {
								if(line[index] == '[') {
									type = Message.Type.Roll;
									var end = line.IndexOf('[', index);
									sender = line.Substring(index, end - index);
								} else {
									if(index + characterName.Length <= line.Length && line.Substring(index, characterName.Length) == characterName) sender = characterName;
									else if(index + name.Length <= line.Length && line.Substring(index, name.Length) == name) sender = name;
									else sender = characterRegex.Match(line, index, Math.Min(20, line.Length - index)).Groups[1].Value;
									index += sender.Length;
									if(index < line.Length) {
										if(line[index] == ':') {
											index += 1;
											if(index < line.Length && line[index] == ' ') index += 1;
										} else {
											type = Message.Type.Action;
										}
									}
								}
								text += index < line.Length ? line.Substring(index) : "";
							}
							var stream = type == Message.Type.Ad ? (ads ?? (ads = File.OpenWrite(Path.Combine(logDir, "Ads")))) :
								(logs ?? (logs = File.OpenWrite(Path.Combine(logDir, "Logs"))));
							if(type == Message.Type.Ad) {
								if(!adsIndexed) {
									serializer.WriteIndex(adsIndex ?? (adsIndex = File.OpenWrite(Path.Combine(logDir, "Ads.idx"))), file.Date, stream.Position);
									adsIndexed = true;
								}
							} else {
								if(!logsIndexed) {
									serializer.WriteIndex(logsIndex ?? (logsIndex = File.OpenWrite(Path.Combine(logDir, "Logs.idx"))), file.Date, stream.Position);
									logsIndexed = true;
								}
							}
							if(sender.Length > 20) sender = sender.Substring(0, 20);
							serializer.Write(stream, new Message(type, new Character(sender), file.Date.AddMinutes(h * 60 + m), text));
						} catch { }
					}
				}
				logs?.Dispose();
				ads?.Dispose();
				logsIndex?.Dispose();
				adsIndex?.Dispose();
			}
		}

		private static IEnumerable<string> GetLogEntries(IEnumerable<string> lines, Regex regex) {
			var builder = new StringBuilder();
			foreach(var l in lines) {
				var line = l;
				if(line.Length > 0 && line[0] == 0) line = new string(line.SkipWhile(x => x == 0).ToArray());
				if(regex.IsMatch(line)) {
					if(builder.Length > 0) {
						yield return builder.ToString();
						builder.Clear();
					}
				}
				if(builder.Length > 0) builder.AppendLine();
				builder.Append(line);
			}
			if(builder.Length > 0) yield return builder.ToString();
		}
	}
}