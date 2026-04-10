using FMODUnity;
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

        [Tooltip("The FMOD event that should play when this level is loaded.")]
        public EventReference MusicTrack;

        [Tooltip("The image that will display for this level in level select UI.")]
        public Texture2D PreviewImage;

        [Tooltip("The FMOD-associated phases related to this combat level.")]
        public Phase[] Phases;

        public bool IsValid =>
            !string.IsNullOrEmpty(this.Name)
            && this.BuildIndex >= 0
            && this.BuildIndex < SceneManager.sceneCountInBuildSettings;
    }
}
