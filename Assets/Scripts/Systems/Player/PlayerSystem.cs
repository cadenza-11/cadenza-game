using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cadenza
{
    /// <summary>
    /// Handles creation, removal, and tracking of players.
    /// </summary>
    [RequireComponent(typeof(PlayerInputManager))]
    public class PlayerSystem : ApplicationSystem
    {
        private static PlayerSystem singleton;

        [Header("Assign in Inspector")]
        [SerializeField] private GameObject characterPrefab;
        [SerializeField] private int maxPlayersInRoster = 4;
        [SerializeField] private CharacterClass[] characterClasses;

        private PlayerInputManager playerInputManager;

        private Dictionary<int, Player> playersByID;
        public static IReadOnlyDictionary<int, Player> PlayersByID => singleton.playersByID;
        public static int PlayerCount => singleton.playersByID.Count;
        public static Player[] Players => PlayersByID.Values.ToArray();
        public static CharacterClass[] CharacterClasses => singleton.characterClasses;

        public static event Action<Player> PlayerJoined;
        public static event Action<Player> PlayerRemoved;
        public static event Action<ScoreDef> PlayerHit;
        public static event Action<Player> PlayerSpawned;

        #region Application Callbacks

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            this.playersByID = new();

            // Configure player input manager.
            this.playerInputManager = this.GetComponent<PlayerInputManager>();
            this.playerInputManager.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed;
            this.playerInputManager.onPlayerJoined += this.OnPlayerJoined;
            this.playerInputManager.onPlayerLeft += this.OnPlayerLeft;
        }

        public override void OnGameStart()
        {
            // Spawn player body.
            foreach (var player in this.playersByID.Values)
                this.SpawnPlayerBody(player);

            // Enable input.
            foreach (var player in this.playersByID.Values)
                player.Input.SwitchCurrentActionMap("Player");
        }

        public override void OnGameStop()
        {
            foreach (var player in this.playersByID.Values)
                player.SetCharacter(null);
        }

        #endregion
        #region Public Static Methods

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

        /// <summary>
        /// Allow new devices/players to join when pressing a button.
        /// </summary>
        public static void EnableJoining()
        {
            singleton.playerInputManager.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenJoinActionIsTriggered;
        }


        /// <summary>
        /// Prevent new devices/players from joining automatically upon button press.
        /// </summary>
        public static void DisableJoining()
        {
            singleton.playerInputManager.joinBehavior = PlayerJoinBehavior.JoinPlayersManually;
        }

        public static int GetCharacterCurrentHealth(int playerID)
        {
            return PlayersByID[playerID].Character.GetCurHealth();
        }

        public static int GetCharacterMaxHealth(int playerID)
        {
            return PlayersByID[playerID].Character.GetCurHealth();
        }

        #endregion
        #region Private Methods

        private void OnPlayerJoined(PlayerInput playerInput)
        {
            playerInput.transform.SetParent(this.transform);
            var player = playerInput.GetComponent<Player>();

            // Configure ID.
            int id = playerInput.playerIndex;
            player.Initialize(id, playerInput);
            this.playersByID[id] = player;

            // Register for hit events.
            player.PlayerHit += (score) => PlayerHit?.Invoke(score);

            Debug.Log($"Player joined using device scheme {playerInput.currentControlScheme}. (id={id})");
            PlayerJoined?.Invoke(player);
        }

        private void OnPlayerLeft(PlayerInput playerInput)
        {
            int id = playerInput.playerIndex;
            var player = this.playersByID[id];
            this.playersByID.Remove(id);

            Debug.Log($"Player using device scheme {playerInput.currentControlScheme} left. (id={id})");
            PlayerRemoved?.Invoke(player);

            Destroy(player);
        }

        private Character SpawnPlayerBody(Player player)
        {
            if (player == null || player.CharacterClass == null || player.CharacterClass.Prefab == null)
                return null;

            var character = Instantiate(player.CharacterClass.Prefab).GetComponent<Character>();
            player.SetCharacter(character);

            Debug.Log($"Player character body set to {character}. (id={player.ID})");
            PlayerSpawned?.Invoke(player);
            return character;
        }

        #endregion
    }
}
