using System.Runtime.Serialization;

namespace CrossBreed.Entities {
	public enum TypingStatusEnum {
		[EnumMember(Value = "clear")] Clear,
		[EnumMember(Value = "paused")] Paused,
		[EnumMember(Value = "typing")] Typing
	}
}