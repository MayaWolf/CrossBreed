using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ML.Collections;
using PropertyChanged;

namespace CrossBreed.Entities {
	[ImplementPropertyChanged]
	public class Channel {
		public IObservableKeyedList<Character, Member> Members { get; }
		public string Id { get; }
		public string Name { get; }
		public string Description { get; set; }
		public ModeEnum Mode { get; set; }
		public event Action ModeChanged;

		public Channel(string id, string name, IObservableKeyedList<Character, Member> members) {
			Id = id;
			Name = name;
			Members = members;
		}

		public enum ModeEnum {
			Chat,
			Ads,
			Both
		}

		public enum RankEnum {
			User,
			Op,
			Owner
		}

		[ImplementPropertyChanged]
		public class Member {
			public Character Character { get; }

			public RankEnum Rank { get; set; }

			public Member(Character character) {
				Character = character;
			}
		}
	}
}