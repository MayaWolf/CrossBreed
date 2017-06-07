using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using CrossBreed.Chat;
using CrossBreed.Entities;
using ML.AppBase;
using ML.Collections;

namespace CrossBreed.ViewModels {
	public class LogsViewModel: BaseViewModel {
		private readonly ILogManager logManager;
		private readonly ObservableList<Message> messages = new ObservableList<Message>();
		private Conversation conversation;
		private Day selectedDay;
		private long logsPosition;
		public IObservableList<MessageViewModel> Messages { get; }
		public IList<Day> Days { get; private set; }

		public Day SelectedDay {
			get => selectedDay;
			set {
				selectedDay = value;
				if(value == null) return;
				if(value.Date == null) {
					logsPosition = -1;
					messages.Reset(logManager.LoadReverse(conversation.LogId, false, 50, ref logsPosition));
				} else {
					messages.Reset(logManager.LoadDay(conversation.LogId, false, value.Date.Value));
				}
			}
		}

		public Conversation SelectedConversation {
			get => conversation;
			set {
				conversation = value;
				var allDay = new Day("-", null);
				var days = new List<Day> { allDay };
				days.AddRange(logManager.GetDays(value.LogId, false).Select(x => new Day(x.ToString(CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern), x)));
				Days = days;
				SelectedDay = allDay;
			}
		}

		public IReadOnlyList<Conversation> Conversations { get; }

		public void LoadMore() {
			if(selectedDay.Date != null) return;
			if(logsPosition != 0) {
				messages.InsertRange(logManager.LoadReverse(conversation.LogId, false, 50, ref logsPosition), 0);
			}
		}

		public LogsViewModel(ILogManager logManager, IChannelManager channelManager) {
			this.logManager = logManager;
			Messages = new UIThreadObservableList<MessageViewModel>(new MappingObservableList<Message, MessageViewModel>(messages, x => {
				return new MessageViewModel(x);
			}));
			Conversations = logManager.GetAllLogIds().Select(x => {
				var name = x;
				if(x[0] == '#') {
					name = x.Substring(1);
					if(name.StartsWith("ADH-", StringComparison.CurrentCultureIgnoreCase) && channelManager.PrivateChannels.ContainsKey(name))
						name = channelManager.PrivateChannels[name].Name;
				}
				return new Conversation(x, name);
			}).ToList();
		}

		public void SetCharacter(Character character) {
			SelectedConversation = Conversations.FirstOrDefault(x => x.LogId == logManager.GetLogId(character));
		}

		public void SetChannel(Channel channel) {
			SelectedConversation = Conversations.FirstOrDefault(x => x.LogId == logManager.GetLogId(channel));
		}

		public class Conversation {
			public string LogId { get; }
			public string Name { get; }
			public Conversation(string logId, string name) {
				LogId = logId;
				Name = name;
			}
		}

		public class Day {
			public string Text { get; }
			public DateTime? Date { get; }
			public Day(string text, DateTime? date) {
				Text = text;
				Date = date;
			}
		}
	}
}