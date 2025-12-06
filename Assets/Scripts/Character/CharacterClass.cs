using UnityEngine;

namespace Cadenza
{
    [CreateAssetMenu(fileName = "NewCharacterClass", menuName = "Cadenza/Character Type", order = 1)]
    public class CharacterClass : ScriptableObject
    {
        public int ID;
        public string Name;
        public Texture2D Portrait;
        public GameObject Prefab;
    }
}
