using System.Collections.Generic;
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

        [SerializeField] private List<Level> levels;

        private ApplicationSystem[] systems;
        private ApplicationState state;
        private bool isRedirecting;
        private Level currentLevel;
        private Level previousLevel;

        /// <summary>
        /// The current state of the application.
        /// </summary>
        public static ApplicationState State => singleton.state;

        /// <summary>
        /// Whether the application is in the process of entering
        /// or exiting from a level.
        /// </summary>
        public static bool IsRedirecting => singleton.isRedirecting;

        /// <summary>
        /// The available list of levels.
        /// </summary>
        public static IReadOnlyList<Level> Levels => singleton.levels;

        /// <summary>
        /// The currently loaded gameplay level. Will not be null during a game session.
        /// </summary>
        public static Level CurrentLevel => singleton.currentLevel;

        /// <summary>
        /// The previous level that was loaded. May be null.
        /// </summary>
        public static Level PreviousLevel => singleton.previousLevel;

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

        public static void SetLevelAsync(Level level)
        {
            _ = singleton.SetLevelAsyncImpl(level);
        }

        public static void RequestQuit()
        {
            if (singleton.state == ApplicationState.Quitting)
                return;

#if UNITY_EDITOR
            singleton.OnApplicationWantsToQuit();
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        private bool OnApplicationWantsToQuit()
        {
            this.ChangeState(ApplicationState.Quitting);
            return true;
        }

        private async Task SetLevelAsyncImpl(Level level)
        {
            if (this.isRedirecting)
            {
                Debug.LogWarning("Attempting to redirect while already redirecting.");
                return;
            }

            this.isRedirecting = true;
            int sceneIndex = 0;

            if (level == null)
                Debug.Log($"Attempted to set a null level. Quitting current level.");
            else if (!level.IsValid)
                Debug.LogWarning($"Attempted to load level {level.Name} (id={level.ID}) with invalid scene. Exiting current level.");
            else
                sceneIndex = level.BuildIndex;

            this.previousLevel = this.currentLevel;
            this.currentLevel = sceneIndex == 0 ? null : level;

            await Fader.ShowAsync();
            singleton.ChangeState(ApplicationState.Pregame);
            {
                // Set the scene.
                Scene currentScene = SceneManager.GetActiveScene();

                // Unload the current scene if it isn't the root scene.
                if (currentScene.buildIndex != 0)
                    await SceneManager.UnloadSceneAsync(currentScene);

                // Load the given scene if it isn't the root scene.
                if (sceneIndex != 0)
                    await SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Additive);

                Scene nextScene = SceneManager.GetSceneByBuildIndex(sceneIndex);
                SceneManager.SetActiveScene(nextScene);
            }
            if (sceneIndex != 0)
                singleton.ChangeState(ApplicationState.GameSession);

            await this.WaitForNextMarkerOrTimeoutAsync();
            this.isRedirecting = false;
            Fader.Hide();
        }

        private async Task WaitForNextMarkerOrTimeoutAsync()
        {
            // The AudioSystem should always respond to a scene change
            // and trigger the next section of the track. This track
            // should always have a starting marker that client can detect.

            // If no marker is passed within a given time frame, timeout.
            const int MarkerTimeOutAsync = 2000;

            await Task.WhenAny(
                Task.Delay(MarkerTimeOutAsync),
                BeatSystem.WaitForNextMarkerAsync()
            );
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
