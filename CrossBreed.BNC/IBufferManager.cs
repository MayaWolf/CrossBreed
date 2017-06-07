using System.Collections.Generic;
using CrossBreed.Entities;

namespace CrossBreed.BNC {
	public interface IBufferManager {
		IEnumerable<ServerCommand> GetBuffer();
		void ClearBuffer();
	}
}
