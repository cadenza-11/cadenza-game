using System;
using Cadenza;
using UnityEngine;

/// <summary>
/// GameManager is reponsible for game flow and logic, including pausing,
/// unpausing, and spawning players.
/// </summary>
public class GameManager : ApplicationSystem
{
    private static GameManager singleton;

    #region Public Variables

    [SerializeField] private Level startingLevel;
    public static event Action<Player> GamePaused;
    public static event Action GameUnpaused;
    public static bool IsPaused => singleton.isPaused;

    #endregion

    private bool isCombatActive;
    private bool isPaused;

    #region Application Callbacks

    public override void OnInitialize()
    {
        Debug.Assert(singleton == null);
        singleton = this;
    }

    public override void OnGameStart()
    {
        AudioSystem.SetState(AudioSystem.State.Game);

        // Spawn players.
        foreach (var player in PlayerSystem.Players)
            PlayerSystem.SpawnPlayerBody(player);

        this.Schedule(5f, () => this.StartCombat());
    }

    public override void OnGameStop()
    {
        AudioSystem.SetState(AudioSystem.State.Menu);

        // Despawn players.
        foreach (var player in PlayerSystem.Players)
            PlayerSystem.DespawnPlayerBody(player);
    }

    public override void OnUpdate()
    {
        if (ApplicationController.State != ApplicationState.GameSession)
            return;

        if (this.isCombatActive && this.CheckWinLoss())
        {
            this.StopCombat();
        }
    }

    #endregion
    #region Public Static Methods

    public static void StartGame()
    {
        if (ApplicationController.State != ApplicationState.Pregame)
            return;

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

    private void StartCombat()
    {
        if (this.isCombatActive)
            return;

        this.isCombatActive = true;

        // Enable input.
        InputSystem.SwitchInputMapMultiPlayer(InputSystem.InputMap.Player);
    }

    private void StopCombat()
    {
        if (!this.isCombatActive)
            return;

        this.isCombatActive = false;

        // Unpause game.
        if (this.isPaused)
        {
            Time.timeScale = 1;
            this.isPaused = false;
        }

        // Disable input.
        InputSystem.SwitchInputMapMultiPlayer(InputSystem.InputMap.UI);
    }

    private bool CheckWinLoss()
    {
        // TODO: Implement
        return false;
    }
}
