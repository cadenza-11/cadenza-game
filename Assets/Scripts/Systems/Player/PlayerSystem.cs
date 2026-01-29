using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

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

        private Player[] players;
        public static Player[] Players => singleton.players;

        public static CharacterClass[] CharacterClasses => singleton.characterClasses;

        public static event Action<Player> PlayerJoined;
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

        public static bool AddPlayer(int id)
        {
            // Join if there is a valid input user but no existing player for a given ID.
            if (InputSystem.TryGetInputUserByID(id, out var input) && !TryGetPlayerByID(id, out _))
            {
                singleton.OnPlayerJoined(input);
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

            singleton.OnPlayerLeft(p.Input);
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

        private void OnPlayerJoined(PlayerInput playerInput)
        {
            var player = playerInput.GetComponent<Player>();

            // Configure ID.
            int id = playerInput.playerIndex;
            this.playersByID[id] = player;
            this.players = this.playersByID.Values.ToArray();

            Debug.Log($"Player joined using device scheme {playerInput.currentControlScheme}. (id={id})");
            PlayerJoined?.Invoke(player);
        }

        private void OnPlayerLeft(PlayerInput playerInput)
        {
            int id = playerInput.playerIndex;
            var player = this.playersByID[id];
            this.playersByID.Remove(id);
            this.players = this.playersByID.Values.ToArray();

            Debug.Log($"Player using device scheme {playerInput.currentControlScheme} left. (id={id})");
            PlayerRemoved?.Invoke(player);
        }

        #endregion
    }
}
