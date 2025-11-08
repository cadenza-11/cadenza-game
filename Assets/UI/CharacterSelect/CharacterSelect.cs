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

        private class PlayerTracker
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
        private int joinedPlayers = 0;

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

            // Create tracker for existing players.
            foreach (var player in PlayerSystem.PlayersByID.Values)
                this.OnPlayerJoined(player);

            // Create tracker for newly joined players.
            PlayerSystem.PlayerJoined += this.OnPlayerJoined;
        }

        public override void Show()
        {
            base.Show();
            this.root.style.display = DisplayStyle.Flex;
            this.submitAction.performed += this.OnSubmit;
        }

        public override void Hide()
        {
            base.Hide();
            this.submitAction.performed -= this.OnSubmit;
            this.root.style.display = DisplayStyle.None;
        }

        private void OnPlayerJoined(Player player)
        {
            Debug.Log("Player joined");
            PlayerTracker newTracker = new()
            {
                Phase = SelectPhase.Joining,
                Container = this.playerContainers[this.joinedPlayers++],
                CalibrationAttempts = -1
            };
            newTracker.TempLabel = newTracker.Container.Q<Label>("temp");
            this.playerPhases.Add(player, newTracker);
        }

        #endregion
        #region Navigation Events

        private void OnSubmit(InputAction.CallbackContext context)
        {
            var player = InputSystem.GetPlayerFromDevice(context.control.device);
            if (player == null || !this.playerPhases.TryGetValue(player, out PlayerTracker foundPlayer))
                return;

            Debug.Log($"Player {player.ID} is navigating. Phase: {foundPlayer.Phase}");

            switch (foundPlayer.Phase)
            {
                case SelectPhase.Joining: // Connect next unassigned container to player. (Done above) //
                    // Call update for container
                    foundPlayer.TempLabel.text = "Name profile here";
                    foundPlayer.Phase++;
                    break;

                case SelectPhase.Calibrating: // For a certain amount of beats, stay on this event until input latency is calculated. //
                    this.Calibrate(player, foundPlayer);
                    break;

                case SelectPhase.CharacterSelection: // Select character. //
                                                     // if (player.Character != null)
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
