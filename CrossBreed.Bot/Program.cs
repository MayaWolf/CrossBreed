using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using CrossBreed.Chat;
using System.Reflection;
using CrossBreed.Net;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json.Linq;
using Websockets;

namespace CrossBreed.Bot {
	class Program {
		private static IApiManager apiManager;
		private static string character;
		private static Type script;
		private static IScript instance;
		private static string userName;

		private static readonly HashSet<string> loaded = new HashSet<string>();

		private static IEnumerable<Assembly> GetAssemblies(Assembly assembly) {
			if(loaded.Contains(assembly.FullName)) yield break;
			loaded.Add(assembly.FullName);
			yield return assembly;
			foreach(var reference in assembly.GetReferencedAssemblies()) {
				Assembly loaded = null;
				try {
					loaded = Assembly.Load(reference);
				} catch(Exception e) {
					Console.WriteLine($"Unable to load assembly {reference.FullName} - {e.Message}");
				}
				if(loaded == null) continue;
				foreach(var item in GetAssemblies(loaded)) yield return item;
			}
		}

		static void Main(string[] args) {
			var refs = GetAssemblies(typeof(Program).Assembly).ToArray();
			WebSocketFactory.Init(() => new WebSocketConnection());
			AppDomain.CurrentDomain.UnhandledException += (_, e) => File.AppendAllText("crash.log", DateTime.Now + "\r\n" + e.ExceptionObject + "\r\n\r\n");
			userName = ConfigurationManager.AppSettings["UserName"];
			if(string.IsNullOrWhiteSpace(userName)) throw new ConfigurationErrorsException("No user configured!");
			character = ConfigurationManager.AppSettings["Character"];
			if(string.IsNullOrWhiteSpace(character)) throw new ConfigurationErrorsException("No character configured!");
			apiManager = new ApiManager();

			var kill = new ManualResetEvent(false);
			Console.CancelKeyPress += delegate { kill.Set(); };
			var path = Path.GetDirectoryName(typeof(object).Assembly.Location);
			var compilation = CSharpCompilation.Create("script",
				new[] { CSharpSyntaxTree.ParseText(File.ReadAllText(ConfigurationManager.AppSettings["Script"])) },
				refs.Select(x => MetadataReference.CreateFromFile(x.Location)),
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
			using(var stream = new MemoryStream()) {
				var result = compilation.Emit(stream);
				if(result.Success) {
					Console.WriteLine("Script compilation successful.");
					script = Assembly.Load(stream.ToArray()).ExportedTypes.Single();
					Connect();
				}
				foreach(var error in result.Diagnostics) Console.WriteLine(error);
			}

			kill.WaitOne();
		}

		private static async void Connect() {
			var response = await apiManager.LogIn(userName, ConfigurationManager.AppSettings["Password"]);
			if(response.TryGetValue("error", out JToken error) && !string.IsNullOrEmpty((string) error)) {
				Console.WriteLine("ERROR: F-List authentication failed!");
				Console.WriteLine(error);
				OnConnectionLoss();
				return;
			}

			var chatManager = new ChatManager(apiManager, typeof(Program).Assembly.GetName());
			var characterManager = new CharacterManager(chatManager, apiManager);
			var channelManager = new ChannelManager(chatManager, characterManager);
			var messageManager = new MessageManager(chatManager, characterManager, channelManager);
			var eventManager = new EventManager(chatManager, messageManager, characterManager, channelManager);
			var logManager = new Logger(chatManager, messageManager);

			var constructor = script.GetConstructors().Single();
			instance = (IScript) constructor.Invoke(constructor.GetParameters().Select<ParameterInfo, object>(x => {
				if(typeof(IApiManager).IsAssignableFrom(x.ParameterType)) return apiManager;
				if(typeof(IChatManager).IsAssignableFrom(x.ParameterType)) return chatManager;
				if(typeof(IChannelManager).IsAssignableFrom(x.ParameterType)) return channelManager;
				if(typeof(ICharacterManager).IsAssignableFrom(x.ParameterType)) return characterManager;
				if(typeof(IMessageManager).IsAssignableFrom(x.ParameterType)) return messageManager;
				if(typeof(IEventManager).IsAssignableFrom(x.ParameterType)) return eventManager;
				throw new Exception("Invalid parameter type " + x.ParameterType);
			}).ToArray());
			instance.Execute();

			chatManager.Disconnected += OnConnectionLoss;
			chatManager.Connect(character, ConfigurationManager.AppSettings["Host"]);
		}

		private static void OnConnectionLoss() {
			Console.WriteLine("Disconnected from F-Chat. Reconnecting in 10 seconds.");
			instance?.Dispose();
			Thread.Sleep(10000);
			Connect();
		}
	}
}