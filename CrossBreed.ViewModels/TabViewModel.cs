using System;
using CrossBreed.Chat;
using CrossBreed.Entities;
using ML.AppBase;
using ML.Collections;
using MvvmCross.Core.ViewModels;
using MvvmCross.Platform.UI;
using PropertyChanged;

namespace CrossBreed.ViewModels {
	public abstract class TabViewModel : BaseViewModel {
		private readonly Func<Command, bool> commandHandler;
		private readonly IChatManager chatManager;
		private readonly MvxColor defaultColor = MvxColors.LightGray;
		private string enteredText;
		private bool isSelected;

		public abstract string Name { get; }

		[AlsoNotifyFor(nameof(BackgroundColor))]
		public bool IsSelected {
			get => isSelected;
			set {
				isSelected = value;
				if(IsSelected) HasNew = false;
			}
		}

		[AlsoNotifyFor(nameof(BackgroundColor))]
		public bool HasNew { get; protected set; }

		public abstract string Image { get; }

		public virtual IMvxCommand SendCommand { get; }

		public string EnteredText {
			get => enteredText;
			set {
				enteredText = value;
				SendCommand.RaiseCanExecuteChanged();
			}
		}

		public string CommandError { get; private set; }

		public event Action EnteredTextChanged;

		public MvxColor BackgroundColor => IsSelected ? MvxColors.AliceBlue : HasNew ? MvxColors.DarkRed : defaultColor;

		public TabViewModel(Func<Command, bool> commandHandler) {
			this.commandHandler = commandHandler;
			SendCommand = new MvxCommand(OnSend, () => !string.IsNullOrEmpty(EnteredText));
		}

		private Command ParseCommand(string command) {
			if(!command.StartsWith("/") || command.StartsWith("/me")) return new Command { Text = command };

			var parts = command.Split(new[] { ' ' }, 2);
			return new Command { Name = parts[0].Substring(1), Text = parts[1].Trim() };
		}

		protected void OnSend() {
			CommandError = "";
			var command = ParseCommand(EnteredText);
			if(OnSend(command)) EnteredText = "";
			else CommandError = Strings.Chat_CommandError;
		}

		protected virtual bool OnSend(Command command) {
			return commandHandler(command);
		}

		public struct Command {
			public string Name;
			public string Text;
		}
	}

	public class HomeTabViewModel : TabViewModel {
		private readonly Lazy<EventsViewModel> events;
		public override string Name => Strings.Chat_Home;
		public override string Image => "res:icon";
		public IObservableList<EventViewModel> Messages => events.Value.Events;

		public HomeTabViewModel(Func<Command, bool> commandHandler, IEventManager eventManager) : base(commandHandler) {
			events = new Lazy<EventsViewModel>(() => new EventsViewModel(eventManager));
		}
	}
}