using UnityEngine;

namespace Cadenza
{
    /// <summary>
    /// An asset that defines a certain subset of all character classes.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterSet", menuName = "Cadenza/Character Set")]
    public class CharacterSet : ScriptableObject
    {
        public CharacterClass[] Values;
        public int MaxID
        {
            get
            {
                int maxID = -1;
                foreach (var charClass in this.Values)
                    maxID = Mathf.Max(maxID, charClass.ID);
                return maxID;
            }
        }
    }
}
