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
        public static Level SelectedLevel => singleton.selectedLevel;

        private bool isCombatActive;
        public static bool IsCombatActive => singleton.isCombatActive;
        public static event Action CombatRequested;
        public static event Action CombatStarted;
        public static event Action<GameResult> CombatStopped;

        private readonly PhaseHandler phaseHandler = new();
        public static Phase? CurrentPhase => singleton.phaseHandler.CurrentPhase;
        public static event Action<Phase> PhaseEntered
        {
            add => singleton.phaseHandler.PhaseEntered += value;
            remove => singleton.phaseHandler.PhaseEntered -= value;
        }
        public static event Action<Phase> PhaseExited
        {
            add => singleton.phaseHandler.PhaseExited += value;
            remove => singleton.phaseHandler.PhaseExited -= value;
        }

        #endregion

        private Level selectedLevel;
        private bool isPaused;

        #region Application Callbacks

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;
        }

        public override void OnGameStart()
        {
            this.RunGameStartAsync();
        }

        public override void OnGameStop()
        {
            // Stop combat.
            if (this.isCombatActive)
                this.StopCombat(GameResult.Forfeit);

            // Reset phases.
            this.phaseHandler.OnGameStop();

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
            if (ApplicationController.CurrentLevel.IsBattleLevel)
                AudioSystem.SetState(AudioSystem.State.Stage);
            else
                AudioSystem.SetState(AudioSystem.State.Backstage);

            // Set phases.
            this.phaseHandler.OnGameStart();

            // Spawn players.
            foreach (var player in PlayerSystem.Players)
                PlayerSystem.SpawnPlayerBody(player);

            // Wait for game to stop transitioning.
            await Fader.WaitUntilHiddenAsync();

            // Enable input.
            InputSystem.SwitchInputMapMultiPlayer(InputSystem.InputMap.Player);

            // Start combat.
            if (ApplicationController.CurrentLevel.IsBattleLevel)
                await this.StartCombatAsync();
        }

        private async void RunGameStartAsync()
        {
            try
            {
                await this.OnGameStartAsync();
            }
            catch (Exception ex)
            {
                // Wait a moment before exiting to the main
                // menu so that other systems' GameStart handlers
                // can finish executing.
                Debug.LogException(ex);
                Timer.ScheduleAsync(0.1f, ExitToPregame);
            }
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

        public static void SetSelectedLevel(Level level)
        {
            singleton.selectedLevel = level;
        }

        public static void RedirectToSelectedLevel()
        {
            if (singleton.selectedLevel != null)
                ApplicationController.SetLevelAsync(singleton.selectedLevel);
        }

        public static void RedirectToBackstage()
        {
            ApplicationController.SetLevelAsync(singleton.startingLevel);
        }

        public static void RestartLevel()
        {
            ApplicationController.SetLevelAsync(ApplicationController.CurrentLevel);
        }

        public static void ExitToPregame()
        {
            if (ApplicationController.State != ApplicationState.GameSession)
                return;

            ApplicationController.SetLevelAsync(null);
        }

        public static bool RequestNextPhase()
        {
            return singleton.phaseHandler.RequestNextPhase();
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

        private async Task StartCombatAsync()
        {
            if (this.isCombatActive)
                return;

            this.isCombatActive = true;

            AudioSystem.SetState(AudioSystem.State.Combat);
            Debug.Log("Combat start requested.");
            InputSystem.SwitchInputMapMultiPlayer(InputSystem.InputMap.UI);

            await BeatSystem.WaitForMarkerAsync("RequestCombat");
            CombatRequested?.Invoke();
            Debug.Log("Combat start acknowledged.");

            await BeatSystem.WaitForMarkerAsync("Combat");
            InputSystem.SwitchInputMapMultiPlayer(InputSystem.InputMap.Player);
            Debug.Log("Starting combat.");
            CombatStarted?.Invoke();
        }

        private void StopCombat(GameResult result)
        {
            if (!this.isCombatActive)
                return;

            this.isCombatActive = false;

            // Set audio.
            AudioSystem.SetState(AudioSystem.State.Stage);

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
