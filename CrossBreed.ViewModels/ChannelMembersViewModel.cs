using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrossBreed.Chat;
using CrossBreed.Entities;
using ML.AppBase;
using ML.Collections;
using MvvmCross.Core.ViewModels;
using MvvmCross.Platform;

namespace CrossBreed.ViewModels {
	public class ChannelMembersViewModel : BaseViewModel<string> {
		private readonly IChannelManager channelManager;
		private readonly ICharacterManager characterManager;

		public Channel Channel { get; private set; }
		public IObservableList<ChannelMemberViewModel> Members { get; private set; }

		public ChannelMemberViewModel SelectedMember { get; set; }
		public event Action SelectedMemberChanged;

		public ChannelMembersViewModel(IChannelManager channelManager, ICharacterManager characterManager) {
			this.channelManager = channelManager;
			this.characterManager = characterManager;
		}

		public ChannelMembersViewModel(IChannelManager channelManager, ICharacterManager characterManager, Channel channel) :
			this(channelManager, characterManager) {
			SetChannel(channel);
		}


		public override async Task Initialize(string channel) {
			SetChannel(channelManager.JoinedChannels[channel]);
		}

		private void SetChannel(Channel channel) {
			Channel = channel;
			Members = new UIThreadObservableList<ChannelMemberViewModel>(new MappingObservableList<Channel.Member, ChannelMemberViewModel>(
				new SortingObservableList<Channel.Member>(Channel.Members, Comparer<Channel.Member>.Create((x1, x2) => Comparer<string>.Default.Compare(x1.Character.Name, x2.Character.Name))),
				member => new ChannelMemberViewModel(characterManager, channelManager, Channel, member)));
		}
	}

	public class ChannelMemberViewModel: BaseViewModel {
		private readonly ICharacterManager characterManager;
		private readonly Channel channel;
		public bool ChannelAdminActionsAvailable => characterManager.OwnCharacter.IsChatOp || channel.Members[characterManager.OwnCharacter].Rank > Channel.RankEnum.User;
		public IMvxCommand ChannelKickCommand { get; }
		public IMvxCommand ChannelBanCommand { get; }

		public string ChannelToggleOpCommandName => Member.Rank == Channel.RankEnum.User ? Strings.Channel_AddOp : Strings.Channel_RemoveOp;
		public IMvxCommand ChannelToggleOpCommand { get; }
		public Channel.Member Member { get; }
		public CharacterViewModel Character { get; }

		public ChannelMemberViewModel(ICharacterManager characterManager, IChannelManager channelManager, Channel channel, Channel.Member member) {
			this.characterManager = characterManager;
			this.channel = channel;
			Member = member;
			Character = Mvx.GetSingleton<CharacterViewModels>().GetCharacterViewModel(member.Character);

			ChannelKickCommand = new MvxCommand(() => channelManager.KickUser(channel, member.Character.Name));
			ChannelBanCommand = new MvxCommand(() => channelManager.SetUserBanned(channel, member.Character.Name, true));
			ChannelToggleOpCommand = new MvxCommand(() => channelManager.SetUserOp(channel, member.Character.Name, Member.Rank == Channel.RankEnum.User));
		}
	}
}