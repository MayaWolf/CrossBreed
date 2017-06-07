using System;
using PropertyChanged;

namespace CrossBreed.Entities {
	[ImplementPropertyChanged]
    public class ChannelListItem {
        public string Id { get; }
        public string Name { get; }
        public int Count { get; set; }
	    public bool IsJoined { get; set; }
		public event Action IsJoinedChanged;

        public ChannelListItem(string id, string name, int count) {
            Id = id;
            Name = name;
            Count = count;
        }
    }
}