using System;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using CrossBreed.Net;
using Websockets;
using WebSocketSharp.Server;

namespace CrossBreed.BNC {
	class Program {
		static void Main() {
			UserManager.Init();
			WebSocketFactory.Init(() => new WebSocketConnection());
			if(int.TryParse(ConfigurationManager.AppSettings["InsecureServerPort"], out int insecurePort)) {
				var insecureServer = new WebSocketServer(insecurePort);
				insecureServer.AddWebSocketService<Connection>("/");
				insecureServer.KeepClean = false;
				insecureServer.Start();
			}
			if(int.TryParse(ConfigurationManager.AppSettings["SecureServerPort"], out int securePort)) {
				var server = new WebSocketServer(securePort, true) {
					SslConfiguration = { ServerCertificate = new X509Certificate2(ConfigurationManager.AppSettings["CertificateFile"], ConfigurationManager.AppSettings["CertificatePassword"]) }
				};
				server.AddWebSocketService<Connection>("/");
				server.KeepClean = false;
				server.Start();
			}
			var kill = new ManualResetEvent(false);
			Console.CancelKeyPress += delegate { kill.Set(); };
			kill.WaitOne();
		}
	}
}
