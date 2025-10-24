using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Cadenza
{
    public class PlayerSystem : ApplicationSystem
    {
        private static PlayerSystem singleton;

        [Header("Test values")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private float playerSpeed;

        private Dictionary<int, Player> playersByID;
        /// <summary>
        /// Assignments of player 1 - 4 by ID.
        /// </summary>
        private int[] roster = { -1, -1, -1, -1 };
        public static IReadOnlyDictionary<int, Player> PlayersByID => singleton.playersByID;
        /// <summary>
        /// Assignments of player 1 - 4 by ID.
        /// </summary>
        public static IReadOnlyCollection<int> PlayerRoster => singleton.roster;
        /// <summary>
        /// Player count in player roster.
        /// </summary>
        public static int PlayerCount
        {
            get
            {
                int value = 0;
                foreach (int i in singleton.roster)
                {
                    if (i > -1) value++;
                }
                return value;
            }
        }
        private Dictionary<int, Vector3> playerFrameImpulsesByID;

        public static event Action<Player> PlayerAdded;

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
            Player player = GetPlayerByID(id);
            if (player != null)
            {
                PlayerInput playerInput = player.Input;
                float accuracy = BeatSystem.GetAccuracy(BeatSystem.CurrentTime);
                float calculatedAccuracy = accuracy > player.Latency ? Math.Abs(accuracy) - Math.Abs(player.Latency) : Math.Abs(Math.Abs(player.Latency) - Math.Abs(accuracy));
                if (playerInput.GetComponentInChildren<AccuracyBar>() is AccuracyBar bar)
                    bar.SetAccuracy(calculatedAccuracy);
                    AudioSystem.PlayOneShotWithParameter(AudioSystem.PlayerOneShotsEvent, "ID", 2);
            }
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
            if (GetPlayerByID(id) != null){
                PlayerInput player = GetPlayerByID(id).Input;
                singleton.playerFrameImpulsesByID[id] = new Vector3(input.x, 0, input.y);
            }
        }

        public override void OnUpdate()
        {
            foreach ((int id, var player) in this.playersByID)
            {
                if (!this.playerFrameImpulsesByID.ContainsKey(id))
                    return;
                player.Input.transform.Translate(this.playerSpeed * Time.deltaTime * this.playerFrameImpulsesByID[id]);
            }
        }

        public static Player GetPlayerByID(int deviceID)
        {
            if (!singleton.playersByID.ContainsKey(deviceID))
                return null;

            return singleton.playersByID[deviceID];
        }

        /// <summary>
        /// Adds player to list of players.
        /// </summary>
        /// <param name="deviceID"></param>
        /// <returns>True: player created and/or added to roster | False: player creation failed</returns>
        public static bool AddPlayer(int deviceID)
        {
            Debug.Log($"Attempting to add Device {deviceID} as player.");
            Player newPlayer = GetPlayerByID(deviceID);
            if (newPlayer == null)
                newPlayer = CreatePlayer(deviceID);

            int deviceIndex = Array.IndexOf(singleton.roster, deviceID);
            int openIndex = Array.IndexOf(singleton.roster, -1);
            if (deviceIndex == -1){
                if (openIndex != -1)
                {
                    Debug.Log($"Device {deviceID} added as Player {openIndex + 1} at index {openIndex}");
                    newPlayer.PlayerNumber = openIndex + 1;
                    singleton.roster[openIndex] = deviceID;
                    return true;
                }
                return false;
            }
            return true;
        }

        public static void RemovePlayer(Player p)
        {
            singleton.roster[p.PlayerNumber - 1] = -1;
            singleton.playersByID.Remove(p.DeviceID);
        }

        public static void RemovePlayer(int id)
        {
            Player p = GetPlayerByID(id);
            singleton.roster[p.PlayerNumber - 1] = -1;
            singleton.playersByID.Remove(id);
        }

        private static Player CreatePlayer(int id)
        {
            GameObject newAvatar = Instantiate(singleton.playerPrefab);
            Player newPlayer = new(newAvatar, id);
            singleton.playersByID[id] = newPlayer;

            Debug.Log($"Player joined with device ID {id}");

            PlayerAdded?.Invoke(newPlayer);

            return newPlayer;
        }
    }
}
