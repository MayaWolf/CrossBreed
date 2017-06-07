using System;
using System.Globalization;
using CodeKicker.BBCode.SyntaxTree;
using CrossBreed.Entities;

namespace CrossBreed.ViewModels {
	public class MessageViewModel {
		private readonly Message message;
		private readonly Lazy<SyntaxTreeNode> formatted;

		public virtual SyntaxTreeNode Formatted => formatted.Value;

		public bool IsBacklog { get; }

		public MessageViewModel(Message message, bool isBacklog = false) {
			this.message = message;
			formatted = new Lazy<SyntaxTreeNode>(CreateFormatted);
			IsBacklog = isBacklog;
		}

		protected MessageViewModel() { }

		private SyntaxTreeNode CreateFormatted() {
			var time = message.Time.ToString(message.Time.Date != DateTime.Now.Date ? "g" : "t", CultureInfo.CurrentUICulture);
			var user = $"[user]{message.Sender.Name}[/user]";
			var text = message.MessageType == Message.Type.Action ? $"[{time}] *{user}{message.Text}" : $"[{time}] {user}: {message.Text}";
			return BbCodeParser.Parse(text);
		}
	}
}