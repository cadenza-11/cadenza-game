using System;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public enum GameState
    {
        InLevel,
        Paused,
        MainMenu,
    }

    public Action<GameState> OnGameStateChanged;

    public void ChangeGameState(GameState newState)
    {
        OnGameStateChanged?.Invoke(newState);
    }
}
