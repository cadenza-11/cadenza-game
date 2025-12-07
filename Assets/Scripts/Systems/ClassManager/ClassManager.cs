using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cadenza
{
    public class ClassManager : ApplicationSystem
    {
        public struct CharacterSelectInfo
        {
            public CharacterClass Class;
            public bool IsTaken;
        }
        private static ClassManager singleton;
        [SerializeField] CharacterClass[] classes;
        private Dictionary<Player, int> takenCharacters = new();
        public static event Action CharacterTakenStatusChanged;

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;
        }

        public static CharacterClass SelectCharacter(Player p, string newClass)
        {
            int index = singleton.ClassNameIndex(newClass);
            if (singleton.takenCharacters.ContainsValue(index) || index == -1)
                return null;
            singleton.takenCharacters.Add(p, index);
            CharacterTakenStatusChanged?.Invoke();
            return singleton.classes[index];
        }

        public static void UnselectCharacter(Player p)
        {
            if(singleton.takenCharacters.TryGetValue(p, out int prevClass))
            {
                CharacterTakenStatusChanged?.Invoke();
            }
            singleton.takenCharacters.Remove(p);
        }

        public static CharacterSelectInfo GetNextCharacter(string currentClass)
        {
            int nextIndex = singleton.ClassNameIndex(currentClass) + 1; // Broken strings (resulting in -1) are automatically set to 0
            if (nextIndex == singleton.classes.Length)
                nextIndex = 0;
            return new CharacterSelectInfo()
            {
                Class = singleton.classes[nextIndex], 
                IsTaken = singleton.takenCharacters.ContainsValue(nextIndex) 
            };
        }

        public static CharacterSelectInfo GetCharacter(string currentClass)
        {
            int index = singleton.ClassNameIndex(currentClass);
            if (index == -1)
                index = 0;
            return new CharacterSelectInfo()
            {
                Class = singleton.classes[index], 
                IsTaken = singleton.takenCharacters.ContainsValue(index) 
            };
        }

        public static CharacterSelectInfo GetPreviousCharacter(string currentClass)
        {
            int previousIndex = singleton.ClassNameIndex(currentClass) - 1;
            if (previousIndex < -1) // Deals with broken strings (-1)
                previousIndex = 0;
            else if (previousIndex < 0) // Deals with class at [0]
                previousIndex = singleton.classes.Length - 1;
            return new CharacterSelectInfo()
            {
                Class = singleton.classes[previousIndex], 
                IsTaken = singleton.takenCharacters.ContainsValue(previousIndex) 
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
    }
}
