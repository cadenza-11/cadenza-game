using FMODUnity;
using UnityEngine;

namespace Cadenza
{
    // Updating any of these enums will modify all SoundCollection ScriptableObjects.
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
            Parry = 0,
            BlockStarted = 1,
            BlockEnded = 2,
            CharacterHit = 3,
            Charge1 = 4,
            Charge2 = 5,
            Charge3 = 6,
            HeavyAttack = 7,
            FailedAttack = 8,
            ComboSucceeded = 9,
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
