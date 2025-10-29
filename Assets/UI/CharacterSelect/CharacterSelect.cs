using System.Collections.Generic;
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
        [SerializeField] private int TotalCallibrationAttempts;

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
            this.root.style.display = DisplayStyle.None;
        }

        public override void Show()
        {
            base.Show();
            this.root.style.display = DisplayStyle.Flex;
            InputSystem.UIInputMap.Get().FindAction("Submit", throwIfNotFound: true).performed += this.OnSubmit;
        }

        public override void Hide()
        {
            base.Hide();
            this.root.style.display = DisplayStyle.None;
        }

        public override void OnStart()
        {
            this.Show();
        }

        #endregion

        #region Navigation Events

        private void OnSubmit(InputAction.CallbackContext context)
        {
            int id = context.control.device.deviceId;

            // Detect new player.
            if (!PlayerSystem.TryGetPlayerByID(id, out Player player))
            {
                if (!PlayerSystem.TryAddPlayer(id, out player))
                    return;

                PlayerTracker newTracker = new()
                {
                    Phase = SelectPhase.Joining,
                    Container = this.playerContainers[player.PlayerNumber - 1],
                    CallibrationAttempts = -1
                };
                newTracker.TempLabel = newTracker.Container.Q<Label>("temp");
                this.playerPhases.Add(player, newTracker);
            }

            PlayerTracker foundPlayer = this.playerPhases[player];
            Debug.Log($"Player {player.PlayerNumber} is navigating. Phase: {foundPlayer.Phase}");
            switch (foundPlayer.Phase)
            {
                case SelectPhase.Joining: // Connect next unassigned container to player. (Done above) //
                    // Call update for container
                    foundPlayer.TempLabel.text = "Name profile here";
                    foundPlayer.Phase++;
                    break;

                case SelectPhase.Callibrating: // For a certain amount of beats, stay on this event until input latency is calculated. //
                    if (foundPlayer.CallibrationAttempts == -1)
                    {
                        foundPlayer.TempLabel.text = "Tap to beat";
                        foundPlayer.CallibrationAttempts++;
                    }
                    else if (foundPlayer.CallibrationAttempts < this.TotalCallibrationAttempts)
                    {
                        float latency = BeatSystem.GetLatency(BeatSystem.CurrentTime);
                        player.Latency = latency;
                        foundPlayer.CallibrationAttempts++;
                    }
                    else if (foundPlayer.CallibrationAttempts == this.TotalCallibrationAttempts)
                    {
                        // Call update for container
                        foundPlayer.TempLabel.text = $"Latency average: {player.Latency}";
                        foundPlayer.Phase++;
                    }
                    break;

                case SelectPhase.CharacterSelection: // Select character. //
                    player.Name = "Temp"; // Update in future.
                    if (!string.IsNullOrEmpty(player.Name))
                        foundPlayer.Phase++;
                    break;

                case SelectPhase.ControllerMapping: // Update controller mapping or select controller mapping profile. //
                    // Figure this out.
                    foundPlayer.TempLabel.text = "Idk controller map here";
                    foundPlayer.Phase++;
                    this.playersReady++;
                    break;

                case SelectPhase.Ready:
                    Debug.Log($"# of players ready: {this.playersReady}/{PlayerSystem.PlayerCount}");
                    if (this.playersReady == PlayerSystem.PlayerCount)
                    {
                        _ = ApplicationController.SetSceneAsync(1);
                        this.Hide();
                    }
                    break;
                default:
                    break;
            }
            this.playerPhases[player] = foundPlayer;
        }
        #endregion

        #region Private Functions
        // Container updates
        #endregion
    }
}
