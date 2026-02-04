using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cadenza
{
    [CreateAssetMenu(fileName = "Level", menuName = "Cadenza/Level")]
    public class Level : ScriptableObject
    {
        [Tooltip("A unique identifier to be used for FMOD.")]
        public int ID;

        [Tooltip("The name of the level.")]
        public string Name;

        [Tooltip("The index of the associated scene in the build settings.")]
        public int BuildIndex;

        [Tooltip("Whether or not this level ")]
        public bool IsBattleLevel;

        public bool IsValid =>
            !string.IsNullOrEmpty(this.Name)
            && this.BuildIndex >= 0
            && this.BuildIndex < SceneManager.sceneCountInBuildSettings;
    }
}
