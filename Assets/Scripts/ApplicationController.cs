using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cadenza
{
    public class ApplicationController : MonoBehaviour
    {
        private static ApplicationController singleton;
        private ApplicationSystem[] systems;

        #region Unity Callbacks

        void Awake()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            // Get application systems.
            this.systems = this.GetComponentsInChildren<ApplicationSystem>();
            Debug.Log($"ApplicationController initialized with {this.systems.Length} systems.");

            // Initialize systems ("Awake")
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

        public static async Task SetSceneAsync(int sceneIndex)
        {
            // Shut down systems.
            foreach (var system in singleton.systems)
                system.OnGameStop();

            // Set the scene.
            await singleton.SetSceneImplAsync(sceneIndex);

            // Shut down systems.
            foreach (var system in singleton.systems)
                system.OnGameStart();
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
    }
}
