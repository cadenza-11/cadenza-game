using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class CharacterSelect : UIPanel
    {
        private enum SelectPhase
        {
            Joining,
            Callibrating,
            CharacterSelection,
            ControllerMapping,
            Ready
        }

        private struct PlayerTracker
        {
            public SelectPhase Phase;
            public VisualElement Container;
        }

        [SerializeField] private UIDocument uiDocument;
        
        private Dictionary<SelectPhase, VisualTreeAsset> screens;
        private VisualElement[] playerContainers;
        private List<VisualElement> usedPlayerContainers = new();

        private Dictionary<int, PlayerTracker> playerPhases;

        #region System Events
        public override void OnInitialize()
        {
            // Set up UI.
            this.root = (TemplateContainer)this.uiDocument.rootVisualElement;
            this.playerContainers = new VisualElement[] {
                this.root.Q<VisualElement>("c_PlayerOne"),
                this.root.Q<VisualElement>("c_PlayerTwo"),
                this.root.Q<VisualElement>("c_PlayerThree"),
                this.root.Q<VisualElement>("c_PlayerFour")
            };
        }

        public override void OnGameStart()
        {
            PlayerSystem.PlayerAdded += OnPlayerAdded;
        }

        public override void OnGameStop()
        {
            PlayerSystem.PlayerAdded -= OnPlayerAdded;
        }

        public override void Show()
        {
            base.Show();
            foreach (KeyValuePair<int, PlayerTracker> player in playerPhases)
            {
                // populate menu idk
            }

            UIActions.FindAction("Submit").performed += OnSubmit;
        }

        public override void Hide()
        {
            base.Hide();
        }
        #endregion

        #region Navigation Events
        private void OnPlayerAdded(int playerIndex)
        {
            if (!playerPhases.ContainsKey(playerIndex))
            {
                PlayerTracker newPlayer = new() { Phase = SelectPhase.Joining, Container = null };
                playerPhases.Add(playerIndex, newPlayer);
            }
        }

        private void OnSubmit(InputAction.CallbackContext context)
        {
            PlayerInput player = PlayerSystem.GetPlayerByID(context.control.device.deviceId);

            if (!playerPhases.TryGetValue(player.playerIndex, out PlayerTracker foundPlayer)) return; // Ignore until user is added
            else
            {
                switch (foundPlayer.Phase)
                {
                    case SelectPhase.Joining: // Connect next unassigned container to player. //
                        int i = 0;
                        while (usedPlayerContainers.Contains(playerContainers[i]) && i < 4) // Find unassigned container (up to 4 containers).
                            i++;
                        if (i < 4) // On success, move on. If a player 5 somehow joined, ignore them.
                        {
                            usedPlayerContainers.Add(playerContainers[i]);
                            foundPlayer.Container = playerContainers[i];
                            // Call update for container
                            foundPlayer.Phase++;
                        }
                        break;
                    case SelectPhase.Callibrating: // For a certain amount of beats, stay on this event until input latency is calculated. //
                        // Idk I still gotta look into this
                        break;
                    case SelectPhase.CharacterSelection: // Select character. //
                        // Add character selection to player system
                        break;
                    case SelectPhase.ControllerMapping: // Update controller mapping or select controller mapping profile. //
                        // Same with this
                        break;
                    case SelectPhase.Ready: // Ignore; just wait. //
                        break;
                    default:
                        break;
                }
            }
        }
        #endregion
        
        #region Private Functions
        // Container updates
        #endregion
    }
}
