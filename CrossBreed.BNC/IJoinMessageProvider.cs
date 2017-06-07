using System.Collections.Generic;
using CrossBreed.Entities;

namespace CrossBreed.BNC {
	public interface IJoinMessageProvider {
		IEnumerable<ServerCommand> GetJoinCommands();
	}
}
