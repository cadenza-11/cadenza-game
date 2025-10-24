using System;
using Cadenza;
using UnityEngine;

public class GameStateManager : ApplicationSystem
{
    private static GameStateManager singleton;
    public enum GameState
    {
        InLevel,
        Paused,
        MainMenu,
    }

    [SerializeField] private UIPanel[] uiDocs;
    private GameState currentState;

    public static event Action<GameState> OnGameStateChanged;

    public override void OnInitialize()
    {
        Debug.Assert(singleton == null);
        singleton = this;
    }

    public static void ChangeGameState(GameState newState)
    {
        if (singleton.currentState == newState) return;
        singleton.currentState = newState;
        OnGameStateChanged?.Invoke(newState);
    }

    public override void OnGameStart()
    {
        this.currentState = GameState.MainMenu;
        this.uiDocs[0].Show();
    }
}
