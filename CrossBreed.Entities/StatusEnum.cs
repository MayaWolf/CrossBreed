using System.Runtime.Serialization;

namespace CrossBreed.Entities {
	public enum StatusEnum {
		[EnumMember(Value = "offline")] Offline,
		[EnumMember(Value = "online")] Online,
		[EnumMember(Value = "away")] Away,
		[EnumMember(Value = "idle")] Idle,
		[EnumMember(Value = "looking")] Looking,
		[EnumMember(Value = "busy")] Busy,
		[EnumMember(Value = "dnd")] DND
	}
}
