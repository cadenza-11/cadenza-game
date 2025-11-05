using UnityEngine;
using UnityEngine.InputSystem;

namespace Cadenza
{
    public class Player
    {
        #region Private Attributes

        private int playerNumber;
        private int deviceID;
        private string characterName; // Replace with character class in future
        private double latency;

        public Transform transform => this.Character.Transform;
        public ICharacter Character { get; private set; }
        public PlayerInput Input { get; private set; }

        #endregion
        #region Public Accessors

        public int DeviceID => this.deviceID;

        public int PlayerNumber
        {
            get { return this.playerNumber; }
            set { this.playerNumber = value; }
        }

        public string Name
        {
            get { return this.characterName; }
            set { this.characterName = value; }
        }

        /// <summary>
        /// Get: returns latency
        /// Set: adds latency and finds average
        /// Use ResetLatency to reset value
        /// </summary>
        public double Latency
        {
            get { return this.latency; }
            set
            {
                if (this.latency == 0) this.latency = value;
                else this.latency = (this.latency + value) / 2;
            }
        }

        #endregion
        #region Functions

        /// <summary>
        /// Tracks this player to an instance of a character body.
        /// </summary>
        /// <param name="character">A spawned instance of the player body.</param>
        public void SetCharacter(ICharacter character)
        {
            this.Character = character;
            this.Input = character.Transform.GetComponent<PlayerInput>();
        }

        public Player(int id)
        {
            this.deviceID = id;
        }

        public void ResetLatency()
        {
            this.latency = 0;
        }

        #endregion
    }
}
