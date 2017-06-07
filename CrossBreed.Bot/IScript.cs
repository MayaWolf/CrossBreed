using System;

namespace CrossBreed.Bot {
	public interface IScript: IDisposable {
		void Execute();
	}
}
