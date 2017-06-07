using System.Linq;
using System.Windows.Input;
using CrossBreed.Chat;
using CrossBreed.Entities;
using ML.AppBase;
using MvvmCross.Core.ViewModels;

namespace CrossBreed.ViewModels {
	public abstract class ConversationSettingsViewModel<TSettings> : BaseViewModel where TSettings : ConversationSettings, new() {
		public bool IsChannel { get; protected set; }
		public TSettings Settings { get; protected set; }
		public ICommand SaveCommand { get; protected set; }

		protected TSettings CloneSettings(TSettings original) => new TSettings {
			Id = original.Id,
			NotifyMessage = CloneNotifySettings(original.NotifyMessage)
		};

		protected NotifySettings CloneNotifySettings(NotifySettings original) => new NotifySettings {
			Notify = original.Notify,
			Flash = original.Flash,
			Sound = original.Sound,
			Toast = original.Toast
		};
	}

	public class ChannelConversationSettingsViewModel : ConversationSettingsViewModel<ChannelConversationSettings> {
		public ChannelConversationSettingsViewModel(Channel channel) {
			IsChannel = true;
			var settings = CharacterSettings.Instance;
			var original = settings.ChannelSettings.FirstOrDefault(x => x.Id == channel.Id);
			if(original != null) {
				Settings = CloneSettings(original);
				Settings.NotifyAd = CloneNotifySettings(original.NotifyAd);
				Settings.NotifyUser = CloneNotifySettings(original.NotifyUser);
			} else Settings = new ChannelConversationSettings();
			SaveCommand = new MvxCommand(() => {
				var list = settings.ChannelSettings.ToList();
				if(original != null) list.Remove(original);
				list.Add(Settings);
				settings.ChannelSettings = list;
			});
		}
	}

	public class PrivateConversationSettingsViewModel : ConversationSettingsViewModel<PrivateConversationSettings> {
		public PrivateConversationSettingsViewModel(Character character) {
			IsChannel = true;
			var settings = CharacterSettings.Instance;
			var original = settings.PrivateSettings.FirstOrDefault(x => x.Id == character.Name);
			if(original != null) {
				Settings = CloneSettings(original);
				Settings.ShowIcon = original.ShowIcon;
			} else Settings = new PrivateConversationSettings();
			SaveCommand = new MvxCommand(() => {
				var list = settings.PrivateSettings.ToList();
				if(original != null) list.Remove(original);
				list.Add(Settings);
				settings.PrivateSettings = list;
			});
		}
	}
}