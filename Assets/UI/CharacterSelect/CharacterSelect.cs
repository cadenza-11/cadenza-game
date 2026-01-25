using System;
using System.Collections.Generic;
using UnityEngine;
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

        [SerializeField] private StartMenu startMenu;
        [SerializeField] private BandNameSelect bandNameSelect;
        [SerializeField] private VisualTreeAsset joiningTemplate;
        [SerializeField] private VisualTreeAsset calibrationTemplate;
        [SerializeField] private VisualTreeAsset characterSelectionTemplate;
        [SerializeField] private VisualTreeAsset readyTemplate;
        [SerializeField] private int TotalCalibrationAttempts;

        #endregion

        #region Variables

        private PlayerContainer[] playerContainers = new PlayerContainer[4];

        private Dictionary<Player, PlayerTracker> playerPhases = new();

        private int playersReady = 0;

        #endregion

        #region System Events

        public override void OnInitialize()
        {
            // Listen for beat.
            BeatSystem.BeatPlayed += this.OnBeat;
        }

        public override void OnShow()
        {
            this.playersReady = 0;
            ClassManager.ClearCharacterAssignments();

            // Reset visuals.
            int i = 0;
            foreach (VisualElement container in this.root.Query<VisualElement>("c_Player").ToList())
            {
                this.ResetContainers(i, container);
                i++;
            }

            // Create tracker for existing players.
            foreach (var player in PlayerSystem.PlayersByID.Values)
                this.OnPlayerJoined(player);

            // Create tracker for newly joined players.
            InputSystem.SwitchInputMapMultiPlayer(InputSystem.InputMap.UI);
            PlayerSystem.PlayerJoined += this.OnPlayerJoined;
            PlayerSystem.EnableJoining();

            // Register for player input.
            InputSystem.UIPlayerSubmit += this.OnSubmit;
            InputSystem.UIPlayerCancel += this.OnCancel;
            InputSystem.UIPlayerNavigate += this.OnMove;

            ClassManager.CharacterTakenStatusChanged += this.RefreshShownCharacters;
        }

        public override void OnHide()
        {
            this.playerPhases = new();
            PlayerSystem.PlayerJoined -= this.OnPlayerJoined;

            // Unregister player input.
            InputSystem.UIPlayerSubmit -= this.OnSubmit;
            InputSystem.UIPlayerCancel -= this.OnCancel;
            InputSystem.UIPlayerNavigate -= this.OnMove;

            ClassManager.CharacterTakenStatusChanged -= this.RefreshShownCharacters;
        }

        private void OnPlayerJoined(Player player)
        {
            if (!this.playerPhases.ContainsKey(player))
            {
                PlayerTracker newTracker = new()
                {
                    Phase = SelectPhase.CalibratingInProgress,
                    Elements = this.playerContainers[player.ID],
                    CalibrationAttempts = 0,
                };
                this.playerPhases.Add(player, newTracker);
            }
            else
            {
                if (this.playerPhases.TryGetValue(player, out PlayerTracker foundPlayer))
                {
                    foundPlayer.Phase = SelectPhase.CalibratingInProgress;
                    foundPlayer.CalibrationAttempts = 0;
                    this.playerPhases[player] = foundPlayer;
                }
            }
            this.ShowPhase(this.playerContainers[player.ID], SelectPhase.CalibratingInProgress);

            // Enable the new player's UI input.
            InputSystem.SwitchInputMapMultiPlayer(InputSystem.InputMap.UI);
        }

        #endregion
        #region Navigation Events

        private void OnSubmit(Player player)
        {
            if (player == null || !this.playerPhases.TryGetValue(player, out PlayerTracker foundPlayer))
                return;

            Debug.Log($"Player {player.ID} is navigating forward. Phase: {foundPlayer.Phase}");

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

                default:
                    break;
            }
        }

        private void OnCancel(Player player)
        {
            if (player == null || !this.playerPhases.TryGetValue(player, out PlayerTracker foundPlayer))
                return;

            Debug.Log($"Player {player.ID} is navigating back. Phase: {foundPlayer.Phase}");

            switch (foundPlayer.Phase)
            {
                case SelectPhase.CalibratingInProgress: // Disconnect. //
                    this.DisconnectPlayer(player, foundPlayer);
                    break;

                case SelectPhase.CalibratingDone: // Back to calibrating. //
                    foundPlayer.CalibrationAttempts = 0;
                    foundPlayer.Phase = SelectPhase.CalibratingInProgress;
                    this.ShowPhase(foundPlayer.Elements, SelectPhase.CalibratingInProgress);
                    break;

                case SelectPhase.CharacterSelection: // Back to calibrating. //
                    foundPlayer.CalibrationAttempts = 0;
                    foundPlayer.Phase = SelectPhase.CalibratingInProgress;
                    this.ShowPhase(foundPlayer.Elements, SelectPhase.CalibratingInProgress);
                    break;

                case SelectPhase.Ready:
                    AudioSystem.SetInstrumentActive(player.CharacterClass, false);
                    player.SetCharacterClass(null);
                    ClassManager.UnselectCharacter(player);
                    this.ShowPhase(foundPlayer.Elements, SelectPhase.CharacterSelection);
                    foundPlayer.Phase = SelectPhase.CharacterSelection;
                    this.playerPhases[player] = foundPlayer;
                    this.playersReady--;
                    break;

                default:
                    break;
            }
        }

        private void OnMove(Vector2 moveDirection, Player player)
        {
            if (player == null
                || !this.playerPhases.TryGetValue(player, out PlayerTracker foundPlayer)
                || foundPlayer.Phase != SelectPhase.CharacterSelection)
                return;
            VisualElement selection = foundPlayer.Elements.CharacterSelectionContainer;
            if (Math.Abs(moveDirection.x) >= Math.Abs(moveDirection.y) && Math.Abs(moveDirection.x) > 0.2f)
            {
                if (selection.Q<VisualElement>("c_CharacterPicker").ClassListContains("is_focus"))
                {
                    String currentChar = selection.Q<Label>("update_CharacterName").text;
                    this.ChangeShownCharacter
                    (
                        selection,
                        (moveDirection.x > 0) ? ClassManager.GetNextCharacter(currentChar) : ClassManager.GetPreviousCharacter(currentChar)
                    );

                }
                else
                    Debug.LogWarning("There are no cosmetics.");
            }
            else if (Math.Abs(moveDirection.y) > 0.2f)
            {
                selection.Q<VisualElement>("c_CharacterPicker").ToggleInClassList("is_focus");
                selection.Q<VisualElement>("c_CosmeticsPicker").ToggleInClassList("is_focus");
            }
        }

        private void OnBeat()
        {
            foreach (VisualElement blinker in this.root.Query<VisualElement>("icon_Blinker").ToList())
            {
                blinker.ToggleInClassList("alt");
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

            phaseContainer.Clear();
            phaseContainer.Add(joining);
            phaseContainer.Add(calibration);
            phaseContainer.Add(selection);
            phaseContainer.Add(ready);

            calibration.Q<Label>("update_MaxAttempts").text = this.TotalCalibrationAttempts.ToString();
            calibration.Q<Label>("update_Counter").text = 0.ToString();

            this.ChangeShownCharacter(selection, ClassManager.GetNextCharacter(""));
            selection.Q<VisualElement>("c_CharacterPicker").AddToClassList("is_focus");
            selection.Q<VisualElement>("c_CosmeticsPicker").RemoveFromClassList("is_focus");

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
                (phase == SelectPhase.Joining)
                ? 0 : 1;
            playerContainer.NavNextHint.style.opacity =
                (phase == SelectPhase.Ready && phase == SelectPhase.CalibratingInProgress)
                ? 0 : 1;
            if (phase == SelectPhase.Joining)
                playerContainer.Container.RemoveFromClassList("joined");
            else
                playerContainer.Container.AddToClassList("joined");
        }

        private void ChangeShownCharacter(VisualElement container, ClassManager.CharacterSelectInfo shownClass)
        {
            container.Q<Label>("update_CharacterName").text = shownClass.Class.Name;
            VisualElement portrait = container.Q<VisualElement>("portrait_Character");
            portrait.style.backgroundImage = shownClass.Class.Portrait;
            VisualElement selector = container.Q<VisualElement>("c_CharacterPicker");
            selector.RemoveFromClassList("taken");
            if (shownClass.IsTaken)
                selector.AddToClassList("taken");
        }

        private void RefreshShownCharacters()
        {
            foreach (VisualElement container in this.root.Query<VisualElement>("c_Character").ToList())
            {
                this.ChangeShownCharacter
                (
                    container,
                    ClassManager.GetCharacter(container.Q<Label>("update_CharacterName").text)
                );
            }

        }

        private void Calibrate(Player player, PlayerTracker tracker)
        {
            if (tracker.Phase == SelectPhase.CalibratingInProgress)
            {
                tracker.Elements.CalibrationContainer.Q<VisualElement>("phase_Calibrating").style.display = DisplayStyle.Flex;
                tracker.Elements.CalibrationContainer.Q<VisualElement>("phase_Results").style.display = DisplayStyle.None;
                this.ShowPhase(tracker.Elements, SelectPhase.CalibratingInProgress);
                var attemptCounter = tracker.Elements.CalibrationContainer.Q<Label>("update_Counter");
                if (tracker.CalibrationAttempts < this.TotalCalibrationAttempts)
                {
                    double latency = BeatSystem.GetLatency(BeatSystem.CurrentTrackTime);
                    ScoreSystem.AddInputLatencyForPlayer(player, latency);
                    tracker.CalibrationAttempts++;
                    attemptCounter.text = tracker.CalibrationAttempts.ToString();
                }
                else if (tracker.CalibrationAttempts == this.TotalCalibrationAttempts)
                {
                    tracker.Elements.CalibrationContainer.Q<VisualElement>("phase_Calibrating").style.display = DisplayStyle.None;
                    tracker.Elements.CalibrationContainer.Q<VisualElement>("phase_Results").style.display = DisplayStyle.Flex;
                    tracker.Elements.CalibrationContainer.Q<Label>("update_Latency").text = ((int)(player.Latency * 1000)).ToString();
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
            CharacterClass selectedCharacter = ClassManager.SelectCharacter
            (
                player,
                tracker.Elements.CharacterSelectionContainer.Q<Label>("update_CharacterName").text
            );

            if (selectedCharacter == null) return; // NOTE: Add animation in future

            player.SetCharacterClass(selectedCharacter);
            this.ShowPhase(tracker.Elements, SelectPhase.Ready);
            AudioSystem.SetInstrumentActive(selectedCharacter, true);
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
                // Open team creation UI if this is a new run.
                if (TeamSystem.Team == null)
                    this.bandNameSelect.Show();
                else
                this.Hide();
            }
                GameManager.StartGame();
        }

        private void DisconnectPlayer(Player player, PlayerTracker tracker)
        {
            this.ResetContainers(player.ID, this.playerContainers[player.ID].Container);
            this.playerPhases.Remove(player);
            if (player.ID == 0)
            {
                this.startMenu.Show();
                this.Hide();
                Debug.LogWarning("Does not reset ALL players.");
            }
        }

        #endregion
    }
}
