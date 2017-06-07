namespace CrossBreed.Entities.ClientMessages {
	public class ClientIgn {
		public Action action { get; set; }
		public string character { get; set; }
		public enum Action {
			add,
			delete,
			notify,
			list
		}
	}
}