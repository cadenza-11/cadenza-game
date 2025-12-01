using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class CharacterSelect : UIPanel
    {
        #region Structures
        private class PlayerContainer
        {
            public VisualElement Container;
            public VisualElement JoiningContainer;
            public VisualElement CalibrationContainer;
            public VisualElement CharacterSelectionContainer;
            public VisualElement ReadyContainer;
            public VisualElement NavBackHint;
            public VisualElement NavNextHint;
        }

        private enum SelectPhase
        {
            Joining,
            CalibratingInProgress,
            CalibratingDone,
            CharacterSelection,
            // ControllerMapping,
            Ready
        }

        private class PlayerTracker
        {
            public SelectPhase Phase;
            public PlayerContainer Elements;
            public int CalibrationAttempts;
        }

        #endregion

        #region Serialized Fields

        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private VisualTreeAsset joiningTemplate;
        [SerializeField] private VisualTreeAsset calibrationTemplate;
        [SerializeField] private VisualTreeAsset characterSelectionTemplate;
        [SerializeField] private VisualTreeAsset readyTemplate;

        [SerializeField] private int TotalCalibrationAttempts;

        #endregion

        #region Variables
        
        private PlayerContainer[] playerContainers = new PlayerContainer[4];

        private Dictionary<Player, PlayerTracker> playerPhases = new();
        private InputAction submitAction;

        private int playersReady = 0;
        private int joinedPlayers = 0;

        #endregion

        #region System Events

        public override void OnInitialize()
        {
            // Set up UI.
            this.root = (TemplateContainer)this.uiDocument.rootVisualElement;
            this.root.style.display = DisplayStyle.None;

            int i = 0;
            foreach (VisualElement container in this.root.Query<VisualElement>("c_Player").ToList())
            {
                this.ResetContainers(i, container);
                i++;
            }

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
            this.submitAction.performed += this.OnSubmit;
            this.root.style.display = DisplayStyle.Flex;
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
                Phase = SelectPhase.CalibratingInProgress,
                Elements = this.playerContainers[this.joinedPlayers],
                CalibrationAttempts = 0,
            };
            this.playerPhases.Add(player, newTracker);
            this.ShowPhase(this.playerContainers[this.joinedPlayers], SelectPhase.CalibratingInProgress);
            this.joinedPlayers++;
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
                case SelectPhase.CalibratingInProgress: // Continue latency calculation process. //
                    this.Calibrate(player, foundPlayer);
                    break;

                case SelectPhase.CalibratingDone: // Proceed to character select. //
                    this.Calibrate(player, foundPlayer);
                    break;

                case SelectPhase.CharacterSelection: // Select character and ready up. //
                                                     // if (player.Character != null)
                    this.SelectCharacter(player, foundPlayer);
                    break;

                // case SelectPhase.ControllerMapping: // Update controller mapping or select controller mapping profile. //
                //     // Figure this out.
                //     foundPlayer.TempLabel.text = "Idk controller map here";
                //     foundPlayer.Phase++;
                //     this.playersReady++;
                //     break;

                default:
                    break;
            }
        }
        #endregion

        #region Private Functions

        private void ResetContainers(int index, VisualElement container)
        {
            var phaseContainer = container.Q<VisualElement>("c_Phase");
            var joining = this.joiningTemplate.Instantiate();
            var calibration = this.calibrationTemplate.Instantiate();
            var selection = this.characterSelectionTemplate.Instantiate();
            var ready = this.readyTemplate.Instantiate();

            phaseContainer.Add(joining);
            phaseContainer.Add(calibration);
            phaseContainer.Add(selection);
            phaseContainer.Add(ready);

            calibration.Q<Label>("update_MaxAttempts").text = this.TotalCalibrationAttempts.ToString();
            calibration.Q<Label>("update_Counter").text = 0.ToString();

            this.playerContainers[index] = new PlayerContainer()
            {
                Container = container,
                JoiningContainer = joining,
                CalibrationContainer = calibration,
                CharacterSelectionContainer = selection,
                ReadyContainer = ready,
                NavBackHint = container.Q<VisualElement>("c_NavBack"),
                NavNextHint = container.Q<VisualElement>("c_NavNext"),
            };

            this.ShowPhase(this.playerContainers[index], SelectPhase.Joining);
        }

        private void ShowPhase(PlayerContainer playerContainer, SelectPhase phase)
        {
            playerContainer.JoiningContainer.style.display = 
                phase == SelectPhase.Joining
                ? DisplayStyle.Flex : DisplayStyle.None;
            playerContainer.CalibrationContainer.style.display = 
                (phase == SelectPhase.CalibratingInProgress || phase == SelectPhase.CalibratingDone)
                ? DisplayStyle.Flex : DisplayStyle.None;
            
            playerContainer.CharacterSelectionContainer.style.display = 
                phase == SelectPhase.CharacterSelection
                ? DisplayStyle.Flex : DisplayStyle.None;
            playerContainer.ReadyContainer.style.display = 
                phase == SelectPhase.Ready
                ? DisplayStyle.Flex : DisplayStyle.None;
            playerContainer.NavNextHint.style.opacity =
                (phase != SelectPhase.Ready && phase != SelectPhase.CalibratingInProgress)
                ? 0 : 1;
            if (phase == SelectPhase.Joining)
                playerContainer.Container.RemoveFromClassList("joined");
            else
                playerContainer.CalibrationContainer.AddToClassList("joined");
        }

        private void Calibrate(Player player, PlayerTracker tracker)
        {
            if (tracker.Phase == SelectPhase.CalibratingInProgress)
            {
                var blinker = tracker.Elements.CalibrationContainer.Q<VisualElement>("c_blinker");
                var attemptCounter = tracker.Elements.CalibrationContainer.Q<Label>("update_Counter");
                if (tracker.CalibrationAttempts < this.TotalCalibrationAttempts)
                {
                    double latency = BeatSystem.GetLatency(BeatSystem.CurrentTrackTime);
                    ScoreSystem.AddInputLatencyForPlayer(player, latency);
                    tracker.CalibrationAttempts++;
                    attemptCounter.text = tracker.CalibrationAttempts.ToString();
                    blinker.ToggleInClassList("alt");
                }
                else if (tracker.CalibrationAttempts == this.TotalCalibrationAttempts)
                {
                    tracker.Elements.CalibrationContainer.Q<VisualElement>("phase_Calibrating").style.display = DisplayStyle.None;
                    tracker.Elements.CalibrationContainer.Q<VisualElement>("phase_Results").style.display = DisplayStyle.Flex;
                    tracker.Elements.CalibrationContainer.Q<Label>("update_Latency").text = ScoreSystem.GetInputLatencyForPlayer(player).ToString();
                    tracker.Phase = SelectPhase.CalibratingDone;
                    this.ShowPhase(tracker.Elements, SelectPhase.CalibratingDone);
                }
            }
            else
            {
                tracker.Phase = SelectPhase.CharacterSelection;
                this.ShowPhase(tracker.Elements, SelectPhase.CharacterSelection);
            }
            this.playerPhases[player] = tracker;
        }

        private void SelectCharacter(Player player, PlayerTracker tracker)
        {
            // Show character being selected
            this.ShowPhase(tracker.Elements, SelectPhase.Ready);
            tracker.Phase = SelectPhase.Ready;
            this.playerPhases[player] = tracker;
            this.ReadyPlayer();
        }

        private void ReadyPlayer()
        {
            this.playersReady++;
            Debug.Log($"# of players ready: {this.playersReady}/{PlayerSystem.PlayerCount}");
            if (this.playersReady == PlayerSystem.PlayerCount)
            {
                _ = ApplicationController.SetSceneAsync(1);
                this.Hide();
            }
        }

        #endregion
    }
}
