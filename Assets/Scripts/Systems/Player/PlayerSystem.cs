using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Cadenza
{
    /// <summary>
    /// Handles creation, removal, and tracking of players.
    /// </summary>
    public class PlayerSystem : ApplicationSystem
    {
        private static PlayerSystem singleton;

        [Header("Assign in Inspector")]
        [SerializeField] private GameObject characterPrefab;
        [SerializeField] private CharacterClass[] characterClasses;

        private Dictionary<int, Player> playersByID;
        public static IReadOnlyDictionary<int, Player> PlayersByID => singleton.playersByID;
        public static int PlayerCount => singleton.playersByID.Count;

        private Player[] players = Array.Empty<Player>();
        public static Player[] Players => singleton.players;

        public static CharacterClass[] CharacterClasses => singleton.characterClasses;

        public static event Action<Player> PlayerAdded;
        public static event Action<Player> PlayerRemoved;
        public static event Action<Player> PlayerSpawned;

        #region Application Callbacks

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            this.playersByID = new();
        }

        #endregion
        #region Public Static Methods

        public static bool AddPlayer(Player player)
        {
            // Add if there is a  but no existing player for a given ID.
            if (InputSystem.IsPlayerJoined(player.ID) && !TryGetPlayerByID(player.ID, out _))
            {
                singleton.OnPlayerAdded(player);
                return true;
            }
            return false;
        }

        public static bool TryGetPlayerByID(int id, out Player player)
        {
            return singleton.playersByID.TryGetValue(id, out player);
        }

        /// <summary>
        /// Attempts to remove a player.
        /// </summary>
        /// <param name="player">The player to remove</param>
        /// <returns>Whether the player exists and was removed successfully</returns>
        public static bool RemovePlayer(Player player)
        {
            return RemovePlayer(player.ID);
        }

        /// <summary>
        /// Attempts to remove a player with the given ID.
        /// </summary>
        /// <param name="id">The ID of the player to remove</param>
        /// <returns>Whether the player exists was removed successfully.</returns>
        public static bool RemovePlayer(int id)
        {
            if (!TryGetPlayerByID(id, out Player p))
                return false;

            singleton.OnPlayerLeft(p);
            return singleton.playersByID.Remove(id);
        }

        public static Character SpawnPlayerBody(Player player)
        {
            if (player == null || player.CharacterClass == null || player.CharacterClass.Prefab == null)
            {
                Debug.Log("Failed to spawn body for player.");
                return null;
            }
            if (player.Character != null)
            {
                Debug.Log("Player already has a body.");
                return player.Character;
            }

            var character = Instantiate(player.CharacterClass.Prefab).GetComponent<Character>();
            player.SetCharacter(character);

            Debug.Log($"Player character body set to {character}. (id={player.ID})");
            PlayerSpawned?.Invoke(player);
            return character;
        }

        public static void DespawnPlayerBody(Player player)
        {
            if (player == null)
            {
                Debug.Log("Failed to spawn body for player.");
                return;
            }

            player.SetCharacter(null);
        }

        #endregion
        #region Private Methods

        private void OnPlayerAdded(Player player)
        {
            this.playersByID[player.ID] = player;
            this.players = this.playersByID.Values.ToArray();

            Debug.Log($"Player registered with PlayerSystem. (id={player.ID})");
            PlayerAdded?.Invoke(player);
        }

        private void OnPlayerLeft(Player player)
        {
            if (!this.playersByID.TryGetValue(player.ID, out _))
                return;

            this.playersByID.Remove(player.ID);
            this.players = this.playersByID.Values.ToArray();

            Debug.Log($"Player unregistered with PlayerSystem. (id={player.ID})");
            PlayerRemoved?.Invoke(player);
        }

        #endregion
    }
}
