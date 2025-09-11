using UnityEngine;

namespace Cadenza
{
    public class ApplicationController : MonoBehaviour
    {
        private ApplicationSystem[] systems;

        void Awake()
        {
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
    }
}
