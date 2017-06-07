using System.Collections.Generic;

namespace CrossBreed.Entities.ServerMessages {
	public class ServerCol {
		public string channel { get; set; }
		public ICollection<string> oplist { get; set; }
	}
}
