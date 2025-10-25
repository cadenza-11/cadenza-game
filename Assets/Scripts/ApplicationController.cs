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

        #region Unity Callbacks

        void Awake()
        {
            Debug.Assert(singleton == null);
            singleton = this;

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
            // Update systems
            foreach (var system in this.systems)
            {
                system.OnUpdate();
            }
        }

        #endregion
        #region Public Static Methods

        // This should be called only by the AudioSystem.
        public static void PlayBeat()
        {
            foreach (var system in singleton.systems)
            {
                system.OnBeat();
            }
        }

        public static async Task SetSceneAsync(int sceneIndex)
        {
            singleton.ChangeState(ApplicationState.Pregame);
            {
                // Set the scene.
                await singleton.SetSceneImplAsync(sceneIndex);
            }
            singleton.ChangeState(ApplicationState.GameSession);
        }

        #endregion

        private async Task SetSceneImplAsync(int sceneIndex)
        {
            var currentScene = SceneManager.GetActiveScene();

            // Unload the current scene if it isn't the root scene.
            if (currentScene.buildIndex != 0)
                await SceneManager.UnloadSceneAsync(currentScene);

            // Load the given scene.
            await SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Additive);

            // Set the scene active.
            Scene loadedScene = SceneManager.GetSceneByBuildIndex(sceneIndex);
            SceneManager.SetActiveScene(loadedScene);
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
                foreach (var system in this.systems)
                    system.OnGameStop();

                return;
            }

            // Starting game.
            if (previousState == ApplicationState.Pregame &&
                newState == ApplicationState.GameSession)
            {
                foreach (var system in this.systems)
                    system.OnGameStart();

                return;
            }

            // Quitting application.
            if (previousState != ApplicationState.Quitting &&
                newState == ApplicationState.Quitting)
            {
                foreach (var system in this.systems)
                    system.OnGameStop();
                foreach (var system in this.systems)
                    system.OnApplicationStop();

                return;
            }
        }
    }
}
