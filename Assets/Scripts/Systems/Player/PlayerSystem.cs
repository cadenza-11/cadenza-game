using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cadenza
{
    public class PlayerSystem : ApplicationSystem
    {
        private static PlayerSystem singleton;

        [Header("Assign in Inspector")]
        [SerializeField] private GameObject playerPrefab;

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

        public static event Action<Player> PlayerAdded;
        public static event Action<Player> PlayerRemoved;

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            this.playersByID = new();
        }

        public override void OnGameStart()
        {
            // Spawn player body.
            foreach (var id in this.roster)
                SpawnPlayer(id);
        }

        public static bool TryGetPlayerByID(int deviceID, out Player player)
        {
            return singleton.playersByID.TryGetValue(deviceID, out player);
        }

        /// <summary>
        /// Creates a new player and adds it to roster.
        /// </summary>
        /// <returns>Whether a new player was successfully added. False is a player already exists with this ID</returns>
        public static bool TryAddPlayer(int deviceID, out Player player)
        {
            // Get existing player.
            if (TryGetPlayerByID(deviceID, out player))
                return false;

            // Create new player.
            player = new(deviceID);
            singleton.playersByID[deviceID] = player;

            // Add to roster at first unused slot.
            int openIndex = Array.IndexOf(singleton.roster, -1);
            if (openIndex != -1)
            {
                singleton.roster[openIndex] = deviceID;
                player.PlayerNumber = openIndex + 1;
            }

            // Notify.
            PlayerAdded?.Invoke(player);
            Debug.Log($"New device joined as Player {player.PlayerNumber}. (id={deviceID})");

            return true;
        }

        /// <summary>
        /// Attempts to remove a player.
        /// </summary>
        /// <param name="player">The player to remove</param>
        /// <returns>Whether the player exists and was removed successfully</returns>
        public static bool RemovePlayer(Player player)
        {
            return RemovePlayer(player.DeviceID);
        }

        /// <summary>
        /// Attempts to remove a player with the given ID.
        /// </summary>
        /// <param name="id">The device ID of the player to remove</param>
        /// <returns>Whether the player exists was removed successfully.</returns>
        public static bool RemovePlayer(int id)
        {
            if (!TryGetPlayerByID(id, out Player p))
                return false;

            singleton.roster[p.PlayerNumber - 1] = -1;
            return singleton.playersByID.Remove(id);
        }

        private static ICharacter SpawnPlayer(int id)
        {
            Debug.Log($"Attempting to spawn player for device ID {id}; stored IDS = {string.Join(',', singleton.playersByID.Keys)}; exists = {TryGetPlayerByID(id, out _)}");
            if (!TryGetPlayerByID(id, out Player player))
                return null;

            Debug.Log($"Player exists:{player.PlayerNumber}");

            var character = Instantiate(singleton.playerPrefab).GetComponent<ICharacter>();
            Debug.Log($"Instantiated {player.PlayerNumber}");
            Debug.Log($"Player is null: {player == null}");
            player.SetCharacter(character);
            Debug.Log("Success");
            Debug.Log($"Spawning player {player.PlayerNumber}");

            return character;
        }
    }
}
