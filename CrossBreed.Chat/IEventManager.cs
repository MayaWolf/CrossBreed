using System;
using CrossBreed.Entities;
using ML.Collections;

namespace CrossBreed.Chat {
	public interface IEventManager {
		event Action<Event> NewEvent;
	}
}