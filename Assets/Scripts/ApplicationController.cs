using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cadenza
{
    public enum ApplicationState
    {
        Booting,
        Pregame,
        GameSession,
        Quitting,
    }

    public class ApplicationController : MonoBehaviour
    {
        private static ApplicationController singleton;
        private ApplicationSystem[] systems;
        private ApplicationState state;
        public static ApplicationState State => singleton.state;
        public static event Action<Player> GamePaused;
        public static event Action GameUnpaused;
        private bool isPaused;
        public static bool IsPaused => singleton.isPaused;

        #region Unity Callbacks

        void Awake()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            Application.wantsToQuit += this.OnApplicationWantsToQuit;

            // Get application systems.
            this.systems = this.GetComponentsInChildren<ApplicationSystem>();
            Debug.Log($"ApplicationController initialized with {this.systems.Length} systems.");

            // Initialize systems ("Awake")
            this.ChangeState(ApplicationState.Pregame);

            foreach (var system in this.systems)
            {
                system.OnInitialize();
            }
        }

        void Start()
        {
            // Start systems ("Start")
            foreach (var system in this.systems)
            {
                system.OnStart();
            }
        }

        void Update()
        {
            // Wait for initialization.
            if (State == ApplicationState.Booting)
                return;

            // Update systems
            foreach (var system in this.systems)
            {
                system.OnUpdate();
            }
        }

        private void StartGame()
        {
            foreach (var system in this.systems)
                system.OnGameStart();
        }

        private void StopGame()
        {
            if (this.isPaused)
            {
                Time.timeScale = 1;
                this.isPaused = false;
            }

            foreach (var system in this.systems)
                system.OnGameStop();
        }

        private void StopApplication()
        {
            this.StopGame();
            foreach (var system in this.systems)
                system.OnApplicationStop();
        }

        #endregion
        #region Public Static Methods

        /// This should be called only by the <see cref="BeatSystem"/>.
        public static void PlayBeat()
        {
            foreach (var system in singleton.systems)
            {
                system.OnBeat();
            }
        }

        public static async Task SetSceneAsync(int sceneIndex)
        {
            await Fader.ShowAsync();
            singleton.ChangeState(ApplicationState.Pregame);
            {
                // Set the scene.
                await singleton.SetSceneImplAsync(sceneIndex);
            }
            if (sceneIndex != 0)
                singleton.ChangeState(ApplicationState.GameSession);
            await Fader.HideAsync();
        }

        public static void ExitToPregame()
        {
            _ = SetSceneAsync(0);
        }

        public static void PauseGame(Player requestingPlayer)
        {
            if (State != ApplicationState.GameSession || singleton.isPaused || requestingPlayer == null)
                return;

            Time.timeScale = 0;
            InputSystem.DisableInputActionMapForPlayers("Player", enableOthers: false, PlayerSystem.Players);
            InputSystem.EnableInputActionMapForPlayers("UI", disableOthers: true, requestingPlayer);

            singleton.isPaused = true;
            Debug.Log($"{requestingPlayer.Name} (id={requestingPlayer.ID}) paused the game.");
            GamePaused?.Invoke(requestingPlayer);
        }

        public static void UnpauseGame()
        {
            if (State != ApplicationState.GameSession || !singleton.isPaused)
                return;

            InputSystem.EnableInputActionMapForPlayers("Player", disableOthers: false, PlayerSystem.Players);
            Time.timeScale = 1;

            singleton.isPaused = false;
            Debug.Log("Game unpaused.");
            GameUnpaused?.Invoke();
        }

        public static void RequestQuit()
        {
            if (singleton.state == ApplicationState.Quitting)
                return;

            Application.Quit();
        }

        #endregion

        private bool OnApplicationWantsToQuit()
        {
            this.ChangeState(ApplicationState.Quitting);
            return true;
        }

        private async Task SetSceneImplAsync(int sceneIndex)
        {
            Scene currentScene = SceneManager.GetActiveScene();
            if (currentScene.buildIndex == sceneIndex)
                return;

            // Unload the current scene if it isn't the root scene.
            if (currentScene.buildIndex != 0)
                await SceneManager.UnloadSceneAsync(currentScene);

            // Load the given scene.
            if (sceneIndex != 0)
                await SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Additive);

            Scene nextScene = SceneManager.GetSceneByBuildIndex(sceneIndex);
            SceneManager.SetActiveScene(nextScene);
        }

        private void ChangeState(ApplicationState newState)
        {
            if (this.state == newState)
                return;

            var previousState = this.state;
            this.state = newState;
            Debug.Log($"Application state changed from {previousState} to {newState}");

            // Exiting game.
            if (previousState == ApplicationState.GameSession &&
                newState == ApplicationState.Pregame)
            {
                this.StopGame();
                return;
            }

            // Starting game.
            if (previousState == ApplicationState.Pregame &&
                newState == ApplicationState.GameSession)
            {
                this.StartGame();
                return;
            }

            // Quitting application.
            if (previousState != ApplicationState.Quitting &&
                newState == ApplicationState.Quitting)
            {
                this.StopApplication();
                return;
            }
        }
    }
}
