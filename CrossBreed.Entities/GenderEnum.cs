using System.Runtime.Serialization;

namespace CrossBreed.Entities {
	public enum GenderEnum {
		None,
		Male,
		Female,
		Shemale,
		Herm,
		[EnumMember(Value = "Male-Herm")] Maleherm,
		[EnumMember(Value = "Cunt-boy")] Cuntboy,
		Transgender
	}
}