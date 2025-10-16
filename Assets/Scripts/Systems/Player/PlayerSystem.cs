using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cadenza
{
    public class PlayerSystem : ApplicationSystem
    {
        private static PlayerSystem singleton;

        [Header("Test values")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private float playerSpeed;

        private Dictionary<int, PlayerInput> playersByID;
        public static IReadOnlyDictionary<int, PlayerInput> PlayersByID => singleton.playersByID;
        private Dictionary<int, Vector3> playerFrameImpulsesByID;

        public static event Action<int> PlayerAdded;

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            this.playersByID = new();
            this.playerFrameImpulsesByID = new();
        }

        public static void OnInteract(int id)
        {
        }

        public static void OnAttackLight(int id)
        {
            PlayerInput player = GetPlayerByID(id);
            // AudioSystem.PlayOneShot(AudioSystem.PlayerOneShotsEvent);
            BeatSystem.GetAccuracy(BeatSystem.CurrentTime);
            AudioSystem.PlayOneShotWithParameter(AudioSystem.PlayerOneShotsEvent, "ID", 2);
        }

        public static void OnAttackHeavy(int id)
        {
        }

        public static void OnAttackSpecial(int id)
        {
        }

        public static void OnAttackTeam(int id)
        {
        }

        public static void OnMove(int id, Vector2 input)
        {
            PlayerInput player = GetPlayerByID(id);
            singleton.playerFrameImpulsesByID[id] = new Vector3(input.x, 0, input.y);
        }

        public override void OnUpdate()
        {
            foreach ((int id, var player) in this.playersByID)
            {
                if (!this.playerFrameImpulsesByID.ContainsKey(id))
                    return;
                player.transform.Translate(this.playerSpeed * Time.deltaTime * this.playerFrameImpulsesByID[id]);
            }
        }

        public static PlayerInput GetPlayerByID(int deviceID)
        {
            if (!singleton.playersByID.ContainsKey(deviceID))
                CreatePlayer(deviceID);

            return singleton.playersByID[deviceID];
        }

        private static GameObject CreatePlayer(int id)
        {
            GameObject newPlayer = Instantiate(singleton.playerPrefab);
            singleton.playersByID[id] = newPlayer.GetComponent<PlayerInput>();
            Debug.Log($"Player joined with device ID {id}");

            PlayerAdded?.Invoke(singleton.playersByID[id].playerIndex);

            return newPlayer;
        }
    }
}
