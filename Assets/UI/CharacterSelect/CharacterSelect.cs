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
            public Label TempLabel; // remove later
            public int CallibrationAttempts;
        }

        [SerializeField] private UIDocument uiDocument;

        private Dictionary<SelectPhase, VisualTreeAsset> screens;
        private VisualElement[] playerContainers;
        private Dictionary<Player, PlayerTracker> playerPhases = new();

        private int playersReady = 0;

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

        public override void Show()
        {
            base.Show();
            this.UIInputMap.FindAction("Submit", throwIfNotFound: true).performed += this.OnSubmit;
        }

        public override void Hide()
        {
            base.Hide();
        }
        #endregion

        #region Navigation Events

        private void OnSubmit(InputAction.CallbackContext context)
        {
            int id = context.control.device.deviceId;
            Player player = PlayerSystem.GetPlayerByID(id);

            if (player == null)
            {
                if (!PlayerSystem.AddPlayer(id)) return;
                player =  PlayerSystem.GetPlayerByID(id);
                PlayerTracker newTracker;
                newTracker.Phase = SelectPhase.Joining;
                newTracker.Container = this.playerContainers[player.PlayerNumber - 1];
                newTracker.CallibrationAttempts = 0;
                newTracker.TempLabel = newTracker.Container.Q<Label>("temp");
                this.playerPhases.Add(player, newTracker);
            }

            PlayerTracker foundPlayer = this.playerPhases[player];
            switch (foundPlayer.Phase)
            {
                case SelectPhase.Joining: // Connect next unassigned container to player. (Done above) //
                    // Call update for container
                    foundPlayer.TempLabel.text = "Name profile here";
                    foundPlayer.Phase++;
                    break;
                case SelectPhase.Callibrating: // For a certain amount of beats, stay on this event until input latency is calculated. //
                    foundPlayer.TempLabel.text = "Tap to beat";
                    if (foundPlayer.CallibrationAttempts < 3)
                    {
                        float accuracy = BeatSystem.GetAccuracy(BeatSystem.CurrentTime);
                        player.Latency = accuracy;
                        foundPlayer.CallibrationAttempts++;
                    }
                    if (foundPlayer.CallibrationAttempts == 3)
                    {
                        // Call update for container
                        foundPlayer.TempLabel.text = $"Accuracy average: {player.Latency}";
                        foundPlayer.Phase++;
                    }
                    break;
                case SelectPhase.CharacterSelection: // Select character. //
                    player.Character = "Temp"; // Update in future.
                    if (!string.IsNullOrEmpty(player.Character))
                        foundPlayer.Phase++;
                    break;
                case SelectPhase.ControllerMapping: // Update controller mapping or select controller mapping profile. //
                    // Figure this out.
                    foundPlayer.TempLabel.text = "Idk controller map here";
                    foundPlayer.Phase++;
                    this.playersReady++;
                    break;
                case SelectPhase.Ready:
                    if (this.playersReady == PlayerSystem.PlayerCount)
                    {
                        GameStateManager.ChangeGameState(GameStateManager.GameState.InLevel);
                        this.Hide();
                    }
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Private Functions
        // Container updates
        #endregion
    }
}
