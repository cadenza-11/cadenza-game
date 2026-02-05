using FMODUnity;
using UnityEngine;

namespace Cadenza
{
    public static class Sound
    {
        public enum UI
        {
            NavSubmit = 0,
            NavMove = 1,
            NavBack = 2,
            InitialJoin = 3,
            CharacterSelected = 4,
        }

        public enum Gameplay
        {
            PlayerAttackLight = 0,
            PlayerAttackHeavy = 1,
            PlayerDamaged = 2,
        }
    }

    [CreateAssetMenu(menuName = "Cadenza/Audio/Sound Collection")]
    public class SoundCollection : ScriptableObject
    {
        [Header("UI Sounds")]
        [SerializeField]
        private EventReference[] uiEvents;

        [Header("Gameplay Sounds")]
        [SerializeField]
        private EventReference[] gameplayEvents;

        public EventReference Get(Sound.UI sound)
        {
            return this.uiEvents[(int)sound];
        }

        public EventReference Get(Sound.Gameplay sound)
        {
            return this.gameplayEvents[(int)sound];
        }
    }
}
