using CrossBreed.Entities;
using ML.Collections;

namespace CrossBreed.Chat {
	public interface ICharacterManager {
		Character OwnCharacter { get; }
		IObservableList<Character> OnlineCharacters { get; }

		Character GetCharacter(string name);
	    void SetIgnored(Character character, bool ignored);
	}
}