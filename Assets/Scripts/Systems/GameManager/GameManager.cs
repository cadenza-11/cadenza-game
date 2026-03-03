using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// GameManager is reponsible for game flow and logic, including pausing,
/// unpausing, and spawning players.
/// </summary>
namespace Cadenza
{
    public class GameManager : ApplicationSystem
    {
        private static GameManager singleton;

        public enum GameResult
        {
            Victory,
            Loss,
            Forfeit,
        }

        #region Public Variables

        [SerializeField] private Level startingLevel;
        [SerializeField] private float combatDelay;

        public static event Action<Player> GamePaused;
        public static event Action GameUnpaused;
        public static bool IsPaused => singleton.isPaused;

        private bool isCombatActive;
        public static bool IsCombatActive => singleton.isCombatActive;
        public static event Action CombatStarted;
        public static event Action<GameResult> CombatStopped;

        #endregion

        private bool isPaused;

        #region Application Callbacks

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;
        }

        public override void OnGameStart()
        {
            _ = this.OnGameStartAsync();
        }

        public override void OnGameStop()
        {
            AudioSystem.SetState(AudioSystem.State.Menu);

            // Stop combat.
            if (this.isCombatActive)
                this.StopCombat(GameResult.Forfeit);

            // Unpause game.
            if (this.isPaused)
            {
                Time.timeScale = 1;
                this.isPaused = false;
            }

            // Disable input.
            InputSystem.SwitchInputMapMultiPlayer(InputSystem.InputMap.UI);

            // Despawn players.
            foreach (var player in PlayerSystem.Players)
                PlayerSystem.DespawnPlayerBody(player);
        }

        private async Task OnGameStartAsync()
        {
            // Set audio.
            AudioSystem.SetState(AudioSystem.State.Game);

            // Spawn players.
            foreach (var player in PlayerSystem.Players)
                PlayerSystem.SpawnPlayerBody(player);

            // Wait for game to stop transitioning.
            await Fader.WaitUntilHiddenAsync();

            // Enable input.
            InputSystem.SwitchInputMapMultiPlayer(InputSystem.InputMap.Player);

            // Start combat.
            if (ApplicationController.CurrentLevel.IsBattleLevel)
                this.StartCombat();
        }

        public override void OnUpdate()
        {
            if (ApplicationController.State != ApplicationState.GameSession || !this.isCombatActive)
                return;

            if (this.CheckLoss())
            {
                this.StopCombat(GameResult.Loss);
            }
            else if (this.CheckVictory())
            {
                this.StopCombat(GameResult.Victory);
            }
        }

        #endregion
        #region Public Static Methods

        public static void RedirectToBackstage()
        {
            ApplicationController.SetLevelAsync(singleton.startingLevel);
        }

        public static void ExitToPregame()
        {
            if (ApplicationController.State != ApplicationState.GameSession)
                return;

            ApplicationController.SetLevelAsync(null);
        }

        public static void PauseGame(Player requestingPlayer)
        {
            if (ApplicationController.State != ApplicationState.GameSession || singleton.isPaused || requestingPlayer == null)
                return;

            Time.timeScale = 0;
            InputSystem.SwitchInputMapSinglePlayer(InputSystem.InputMap.UI, requestingPlayer);

            singleton.isPaused = true;
            Debug.Log($"{requestingPlayer.Name} (id={requestingPlayer.ID}) paused the game.");
            GamePaused?.Invoke(requestingPlayer);
        }

        public static void UnpauseGame()
        {
            if (ApplicationController.State != ApplicationState.GameSession || !singleton.isPaused)
                return;

            InputSystem.SwitchInputMapMultiPlayer(InputSystem.InputMap.Player);
            Time.timeScale = 1;

            singleton.isPaused = false;
            Debug.Log("Game unpaused.");
            GameUnpaused?.Invoke();
        }

        #endregion
        #region Combat Methods

        private void StartCombat()
        {
            if (this.isCombatActive)
                return;

            this.isCombatActive = true;

            Debug.Log("Starting combat.");
            CombatStarted?.Invoke();
        }

        private void StopCombat(GameResult result)
        {
            if (!this.isCombatActive)
                return;

            this.isCombatActive = false;
            Debug.Log($"Stopping combat with result {result}.");
            CombatStopped?.Invoke(result);
        }

        private bool CheckLoss()
        {
            // All players are downed.
            foreach (var player in PlayerSystem.Players)
            {
                if (player.Character?.currentHealth > 0)
                    return false;
            }

            return true;
        }

        private bool CheckVictory()
        {
            // No enemies remain.
            if (EnemyManager.EnemyCount > 0)
                return false;

            return true;
        }

        #endregion
    }
}
