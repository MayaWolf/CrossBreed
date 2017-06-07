using System.Collections.Generic;

namespace CrossBreed.Entities.ServerMessages {
	public class ServerIgn {
		public string action { get; set; }

		public string character { get; set; }

		public IReadOnlyCollection<string> characters { get; set; }
	}
}
