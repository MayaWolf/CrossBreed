using System;
using CrossBreed.Bot;
using CrossBreed.Chat;
using CrossBreed.Entities;

public class Script : IScript {
	private readonly IApiManager apiManager;
	private readonly IChatManager chatManager;
	private readonly IChannelManager channelManager;
	private readonly ICharacterManager characterManager;
	private readonly IEventManager eventManager;
	private readonly IMessageManager messageManager;

	public Script(IApiManager apiManager, IChatManager chatManager, IChannelManager channelManager, ICharacterManager characterManager, IEventManager eventManager, IMessageManager messageManager) {
		this.apiManager = apiManager;
		this.chatManager = chatManager;
		this.channelManager = channelManager;
		this.characterManager = characterManager;
		this.eventManager = eventManager;
		this.messageManager = messageManager;
	}

	/// <summary>
	/// Register event handlers and define script logic here.
	/// </summary>
	public void Execute() {
		messageManager.ChannelMessageReceived += (channel, message) => {
		};
	}

	/// <summary>
	/// Cleanup. Event handlers don't need to be unregistered most of the time.
	/// </summary>
	public void Dispose() { }
}