using System;
using System.ComponentModel;
using PropertyChanged;

namespace CrossBreed.Entities {
	[ImplementPropertyChanged]
	public class Character {
		public string Name { get; }
		public GenderEnum Gender { get; set; }
		public StatusEnum Status { get; set; }
		public string StatusMessage { get; set; }
		public bool IsFriend { get; set; }
		public event Action IsFriendChanged;
		public bool IsBookmarked { get; set; }
		public event Action IsBookmarkedChanged;
		public bool IsChatOp { get; set; }
		public event Action IsChatOpChanged;
		public bool IsIgnored { get; set; }
		public event Action IsIgnoredChanged;
		public TypingStatusEnum TypingStatus { get; set; }

		public Character(string name) {
			Name = name;
		}
	}
}
