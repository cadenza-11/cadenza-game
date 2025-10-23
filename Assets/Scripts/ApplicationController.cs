using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

namespace Cadenza
{
    public enum ApplicationState
    {
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

            // Load the first scene in the build index.
            SetSceneAsync(1);
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
            // Exiting game.
            if (this.state == ApplicationState.GameSession &&
                newState == ApplicationState.Pregame)
            {
                foreach (var system in this.systems)
                    system.OnGameStop();
            }

            // Starting game.
            else if (
                this.state == ApplicationState.Pregame &&
                newState == ApplicationState.GameSession)
            {
                foreach (var system in this.systems)
                    system.OnGameStart();
            }

            // Quitting application.
            else if (
                this.state != ApplicationState.Quitting &&
                newState == ApplicationState.Quitting)
            {
                foreach (var system in this.systems)
                    system.OnGameStop();
                foreach (var system in this.systems)
                    system.OnApplicationStop();
            }

            Debug.Log($"Application state changed from {this.state} to {newState}");
            this.state = newState;
        }
    }
}

#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
