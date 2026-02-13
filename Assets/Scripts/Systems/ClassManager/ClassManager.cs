using System;
using System.Collections.Generic;

namespace Cadenza
{
    /// <summary>
    /// Used for UI management of characters by the <see cref="CharacterSelect"> panel.
    /// </summary>
    public class UIClassManager
    {
        public struct CharacterSelectInfo
        {
            public CharacterClass Class;
            public bool IsTaken;
        }

        private Dictionary<Player, int> takenCharacters = new();
        public event Action CharacterTakenStatusChanged;
        private CharacterClass[] classes => TeamSystem.AvailableClasses.Values;

        public CharacterClass SelectCharacter(Player p, string newClass)
        {
            int index = this.ClassNameIndex(newClass);
            if (this.takenCharacters.ContainsValue(index) || index == -1)
                return null;
            this.takenCharacters.Add(p, index);
            CharacterTakenStatusChanged?.Invoke();
            return this.classes[index];
        }

        public void UnselectCharacter(Player p)
        {
            if (this.takenCharacters.TryGetValue(p, out int prevClass))
                CharacterTakenStatusChanged?.Invoke();

            this.takenCharacters.Remove(p);
        }

        public CharacterSelectInfo GetNextCharacter(string currentClass)
        {
            int nextIndex = this.ClassNameIndex(currentClass) + 1; // Broken strings (resulting in -1) are automatically set to 0
            if (nextIndex == this.classes.Length)
                nextIndex = 0;
            return new CharacterSelectInfo()
            {
                Class = this.classes[nextIndex],
                IsTaken = this.takenCharacters.ContainsValue(nextIndex)
            };
        }

        public CharacterSelectInfo GetCharacter(string currentClass)
        {
            int index = this.ClassNameIndex(currentClass);
            if (index == -1)
                index = 0;
            return new CharacterSelectInfo()
            {
                Class = this.classes[index],
                IsTaken = this.takenCharacters.ContainsValue(index)
            };
        }

        public CharacterSelectInfo GetPreviousCharacter(string currentClass)
        {
            int previousIndex = this.ClassNameIndex(currentClass) - 1;
            if (previousIndex < -1) // Deals with broken strings (-1)
                previousIndex = 0;
            else if (previousIndex < 0) // Deals with class at [0]
                previousIndex = this.classes.Length - 1;
            return new CharacterSelectInfo()
            {
                Class = this.classes[previousIndex],
                IsTaken = this.takenCharacters.ContainsValue(previousIndex)
            };
        }

        private int ClassNameIndex(string className)
        {
            for (int i = 0; i < this.classes.Length; i++)
            {
                if (this.classes[i].Name == className)
                    return i;
            }
            return -1;
        }

        public void ClearCharacterAssignments()
        {
            this.takenCharacters.Clear();
        }
    }
}
