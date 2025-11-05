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
            Calibrating,
            CharacterSelection,
            ControllerMapping,
            Ready
        }

        private struct PlayerTracker
        {
            public SelectPhase Phase;
            public VisualElement Container;
            public Label TempLabel; // remove later
            public int CalibrationAttempts;
        }

        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private int TotalCalibrationAttempts;

        private Dictionary<SelectPhase, VisualTreeAsset> screens;
        private VisualElement[] playerContainers;
        private Dictionary<Player, PlayerTracker> playerPhases = new();
        private InputAction submitAction;

        private int playersReady = 0;

        #region System Events
        public override void OnInitialize()
        {
            // Set up UI.
            this.root = (TemplateContainer)this.uiDocument.rootVisualElement;
            this.root.style.display = DisplayStyle.None;

            this.playerContainers = new VisualElement[] {
                this.root.Q<VisualElement>("c_PlayerOne"),
                this.root.Q<VisualElement>("c_PlayerTwo"),
                this.root.Q<VisualElement>("c_PlayerThree"),
                this.root.Q<VisualElement>("c_PlayerFour")
            };

            this.submitAction = InputSystem.UIInputMap.Get().FindAction("Submit", throwIfNotFound: true);
            this.root.style.display = DisplayStyle.None;
        }

        public override void Show()
        {
            base.Show();
            this.root.style.display = DisplayStyle.Flex;
            this.submitAction.performed += this.OnSubmit;
            foreach (int id in PlayerSystem.PlayerRoster)
            {
                if (!PlayerSystem.TryGetPlayerByID(id, out Player player)) continue;
                PlayerTracker newTracker = new()
                {
                    Phase = SelectPhase.Calibrating,
                    Container = this.playerContainers[player.PlayerNumber - 1],
                    CalibrationAttempts = -1
                };
                newTracker.TempLabel = newTracker.Container.Q<Label>("temp");
                this.playerPhases.Add(player, newTracker);
                this.Calibrate(player, newTracker);
            }
        }

        public override void Hide()
        {
            base.Hide();
            this.submitAction.performed -= this.OnSubmit;
            this.root.style.display = DisplayStyle.None;
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
                    CalibrationAttempts = -1
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
                    this.playerPhases[player] = foundPlayer;
                    break;

                case SelectPhase.Calibrating: // For a certain amount of beats, stay on this event until input latency is calculated. //
                    this.Calibrate(player, foundPlayer);
                    break;

                case SelectPhase.CharacterSelection: // Select character. //
                    player.Name = "Temp"; // Update in future.
                    if (!string.IsNullOrEmpty(player.Name))
                        foundPlayer.Phase++;
                    this.playerPhases[player] = foundPlayer;
                    break;

                case SelectPhase.ControllerMapping: // Update controller mapping or select controller mapping profile. //
                    // Figure this out.
                    foundPlayer.TempLabel.text = "Idk controller map here";
                    foundPlayer.Phase++;
                    this.playersReady++;
                    this.playerPhases[player] = foundPlayer;
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
        }
        #endregion

        #region Private Functions

        private void Calibrate(Player player, PlayerTracker tracker)
        {
            if (tracker.CalibrationAttempts == -1)
            {
                tracker.TempLabel.text = "Tap to beat";
                tracker.CalibrationAttempts++;
            }
            else if (tracker.CalibrationAttempts < this.TotalCalibrationAttempts)
            {
                double latency = BeatSystem.GetLatency(BeatSystem.CurrentTrackTime);
                player.Latency = latency;
                tracker.CalibrationAttempts++;
            }
            else if (tracker.CalibrationAttempts == this.TotalCalibrationAttempts)
            {
                ScoreSystem.SetInputLatencyForPlayer(player, player.Latency);

                // Call update for container
                tracker.TempLabel.text = $"Latency average: {player.Latency}";
                tracker.Phase = SelectPhase.CharacterSelection;
            }
            this.playerPhases[player] = tracker;
        }

        // Container updates
        #endregion
    }
}
