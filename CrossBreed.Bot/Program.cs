using System;
using System.CodeDom.Compiler;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using CrossBreed.Chat;
using System.Reflection;
using CrossBreed.Net;
using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using Newtonsoft.Json.Linq;
using Websockets;

namespace CrossBreed.Bot {
	class Program {
		private static IApiManager apiManager;
		private static string character;
		private static Type script;
		private static IScript instance;
		private static string userName;

		private static readonly CompilerParameters compilerParameters = new CompilerParameters(new[] {
			"mscorlib.dll",
			"System.Core.dll",
			"System.dll",
			"System.Runtime.dll",
			"System.ObjectModel.dll",
			"CrossBreed.Chat.dll",
			"CrossBreed.Entities.dll",
			"CrossBreed.Bot.exe",
			"ML.Collections.dll"
		});

		static void Main(string[] args) {
			WebSocketFactory.Init(() => new WebSocketConnection());
			AppDomain.CurrentDomain.UnhandledException += (_, e) => File.AppendAllText("crash.log", DateTime.Now + "\r\n" + e.ExceptionObject + "\r\n\r\n");
			userName = ConfigurationManager.AppSettings["UserName"];
			if(userName == null) throw new ConfigurationErrorsException("No user configured!");
			character = ConfigurationManager.AppSettings["Character"];
			if(character == null) throw new ConfigurationErrorsException("No character configured!");
			apiManager = new ApiManager();

			var kill = new ManualResetEvent(false);
			Console.CancelKeyPress += delegate { kill.Set(); };

			var compiler = new CSharpCodeProvider();
			var results = compiler.CompileAssemblyFromFile(compilerParameters, ConfigurationManager.AppSettings["Script"]);
			var errors = results.Errors.Cast<CompilerError>().ToList();
			if(errors.Count > 0) {
				foreach(var error in errors) Console.WriteLine(error.ErrorText);
			} else {
				script = results.CompiledAssembly.ExportedTypes.Single();
				Connect();
			}

			kill.WaitOne();
		}

		private static async void Connect() {
			var response = await apiManager.LogIn(userName, ConfigurationManager.AppSettings["Password"]);
			if(response.TryGetValue("error", out JToken error) && error.HasValues) {
				Console.WriteLine("ERROR: F-List authentication failed!");
				OnConnectionLoss();
				return;
			}

			var chatManager = new ChatManager(apiManager);
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