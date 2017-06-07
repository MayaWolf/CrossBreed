namespace CrossBreed.Entities.ClientMessages {
	public class ClientIdn {
		public string method { get; set; } = "ticket";
		public string account { get; set; }
		public string ticket { get; set; }
		public string character { get; set; }
		public string cname { get; set; }
		public string cversion { get; set; }
	}
}
