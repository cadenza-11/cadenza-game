using UnityEngine;
using UnityEngine.InputSystem;

namespace Cadenza
{
    public class Player : MonoBehaviour
    {
        #region Private Attributes

        private int playerNumber;
        private int deviceID;
        private string profileName; // Replace with profile class in future
        private string characterName; // Replace with character class in future
        private float latency;
        private GameObject avatar;
        public PlayerInput Input { get; private set; }

        #endregion
        #region Public Accessors

        public int DeviceID => this.deviceID;

        public int PlayerNumber
        {
            get { return this.playerNumber; }
            set { this.playerNumber = value; }
        }

        public string Profile
        {
            get { return this.profileName; }
            set { this.profileName = value; }
        }

        public string Character
        {
            get { return this.characterName; }
            set { this.characterName = value; }
        }

        /// <summary>
        /// Get: returns latency
        /// Set: adds latency and finds average
        /// Use ResetLatency to reset value
        /// </summary>
        public float Latency
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

        public Player(GameObject av, int id)
        {
            this.avatar = av;
            this.Input = av.GetComponent<PlayerInput>();
            this.deviceID = id;
        }

        public void ResetLatency()
        {
            this.latency = 0;
        }

        #endregion
    }
}