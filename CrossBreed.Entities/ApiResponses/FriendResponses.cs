using System.Collections.Generic;

namespace CrossBreed.Entities.ApiResponses {
	public class FriendListItem {
		public string source { get; set; }
		public string dest { get; set; }
		public int? id { get; set; }
	}

	public class FriendListResponse {
		public IReadOnlyCollection<FriendListItem> friends { get; set; }
	}

	public class FriendRequestsResponse {
		public IReadOnlyCollection<FriendListItem> requests { get; set; }
	}
}