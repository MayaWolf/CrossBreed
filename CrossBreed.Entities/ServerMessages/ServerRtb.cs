namespace CrossBreed.Entities.ServerMessages {
	public class ServerRtb {
		public Type type { get; set; }
		public string name { get; set; }

		public enum Type {
			friendadd,
			friendremove,
			friendrequest,
			trackadd,
			trackrem,
			note,
			comment,
			grouprequest,
			bugreport,
			helpdeskticket,
			helpdeskreply,
			featurerequest
		}

		public enum CommentTargetType {
			newspost,
			bugreport,
			changelog,
			feature
		}
	}
}