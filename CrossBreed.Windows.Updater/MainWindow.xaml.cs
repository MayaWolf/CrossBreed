using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CrossBreed.Windows.Updater {
	public partial class MainWindow : Window {
		public static readonly DependencyProperty CurrentActionProperty = DependencyProperty.Register(nameof(CurrentAction), typeof(string), typeof(MainWindow));
		public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(nameof(Progress), typeof(int), typeof(MainWindow));
		private string targetDirectory;
		private string uri;
		private string hash;
		private bool closing;
		public const int PatcherVersion = 1;

		public string CurrentAction {
			get { return (string) GetValue(CurrentActionProperty); }
			set { SetValue(CurrentActionProperty, value); }
		}

		public int Progress {
			get { return (int) GetValue(ProgressProperty); }
			set { SetValue(ProgressProperty, value); }
		}

		public MainWindow() {
			AppDomain.CurrentDomain.UnhandledException += HandleException;
			Title = Strings.Title;
			InitializeComponent();
			DoUpdate();
		}

		private async void DoUpdate() {
			var parameters = Environment.GetCommandLineArgs();
			if(parameters.Length == 4) {
				targetDirectory = parameters[1];
				uri = parameters[2];
				hash = parameters[3];
			} else {
				var data = await new UpdateChecker().CheckForUpdate(0);
				if(data.PatcherVersion > PatcherVersion) {
					if(MessageBox.Show(this, Strings.WarningWindow, Strings.UpdaterOutdated, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes) {
						Process.Start(data.Patcher);
					}
					Close();
					return;
				}
				uri = data.Url;
				targetDirectory = Path.Combine(Directory.GetCurrentDirectory(), "CrossBreed");
				hash = data.FileHash;
			}
			CurrentAction = Strings.Action_DownloadingFile;
			using(var client = new WebClient()) {
				client.DownloadProgressChanged += (s, e) => Progress = e.ProgressPercentage / 2;
				client.DownloadDataCompleted += FileDownloaded;
				client.DownloadDataAsync(new Uri(uri));
			}
		}

		protected override void OnClosing(CancelEventArgs e) {
			e.Cancel = !closing;
		}

		private new void Close() {
			closing = true;
			base.Close();
		}

		private void FileDownloaded(object sender, DownloadDataCompletedEventArgs args) {
			CurrentAction = Strings.Action_CheckingFile;
			using(var md5 = MD5.Create()) {
				var computedHash = BitConverter.ToString(md5.ComputeHash(args.Result)).Replace("-", "");
				if(computedHash != hash) {
					MessageBox.Show(this, Strings.HashMismatch, Strings.ErrorWindow, MessageBoxButton.OK, MessageBoxImage.Error);
					Close();
					return;
				}
			}
			Progress = 65;
			if(Directory.Exists(targetDirectory)) {
				CurrentAction = Strings.Action_CleaningDirectory;
				if(MessageBox.Show(this, string.Format(Strings.CleaningNotice, targetDirectory), Strings.WarningWindow,
					MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.Cancel) {
					Close();
					return;
				}
				foreach(var file in Directory.EnumerateFiles(targetDirectory)) File.Delete(file);
				Progress = 70;
				foreach(var subDirectory in Directory.EnumerateDirectories(targetDirectory)) Directory.Delete(subDirectory, true);
				Progress = 80;
			} else {
				Directory.CreateDirectory(targetDirectory);
				Progress = 80;
			}
			CurrentAction = Strings.Action_Installing;
			using(var stream = new MemoryStream(args.Result))
			using(var zip = new ZipArchive(stream)) {
				zip.ExtractToDirectory(targetDirectory);
			}
			Progress = 100;
			CurrentAction = Strings.Action_Completed;
			Task.Delay(1000).ContinueWith(r => {
				Process.Start(Path.Combine(targetDirectory, "CrossBreed.Windows.exe"));
				Dispatcher.Invoke(Close);
			});
		}

		private void HandleException(object sender, UnhandledExceptionEventArgs args) {
			File.AppendAllText("crash.log", DateTime.Now + "\r\n" + args.ExceptionObject + "\r\n\r\n");
			File.AppendAllText(Path.Combine(targetDirectory, "updater.crash.log"), DateTime.Now + "\r\n" + args.ExceptionObject + "\r\n\r\n");
		}
	}
}