using UnityEngine;

namespace Cadenza
{
    [CreateAssetMenu(fileName = "NewColorway", menuName = "Cadenza/Colorway")]
    public class Colorway : ScriptableObject
    {
        public string Name;
        public Color PrimaryColor;
        public Color SecondaryColor;
        public Color TertiaryColor;
        public Texture2D DisplayImage;
    }
}
