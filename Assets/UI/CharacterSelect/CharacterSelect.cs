using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class CharacterSelect : UIPanel
    {
        #region Structures

        /// <summary>
        /// Houses UXML elements pertaining to a player.
        /// </summary>
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

        /// <summary>
        /// Houses player-related data needed for UI.
        /// </summary>
        private class PlayerTracker
        {
            public SelectPhase Phase;
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
        [SerializeField] private int requiredCalibrationAttempts;

        #endregion

        #region Variables

        private PlayerContainer[] playerContainers;
        private Dictionary<Player, PlayerTracker> playerTrackers = new();

        #endregion

        #region System Events

        public override void OnInitialize()
        {
            // Only allow as many player containers as what is defined in the UXML.
            var containers = this.root.Query<VisualElement>("c_Player").ToList();
            this.playerContainers = new PlayerContainer[containers.Count];

            // Bind a new PlayerContainer to the container element.
            for (int i = 0; i < containers.Count; i++)
                this.playerContainers[i] = this.GetNewContainer(containers[i]);
        }

        public override void OnShow()
        {
            ClassManager.ClearCharacterAssignments();

            // Reset views.
            foreach (var container in this.playerContainers)
                this.ResetContainerView(container);

            // Create tracker for existing players.
            foreach (var player in PlayerSystem.PlayersByID.Values)
                this.OnPlayerJoined(player);

            // Create tracker for newly joined players.
            InputSystem.SwitchInputMapMultiPlayer(InputSystem.InputMap.UI);
            PlayerSystem.PlayerJoined += this.OnPlayerJoined;
            PlayerSystem.EnableJoining();

            // Handle player disconnect.
            PlayerSystem.PlayerRemoved += this.DisconnectPlayer;

            // Register for player input.
            InputSystem.UIPlayerSubmit += this.OnSubmit;
            InputSystem.UIPlayerCancel += this.OnCancel;
            InputSystem.UIPlayerNavigate += this.OnNavigate;

            ClassManager.CharacterTakenStatusChanged += this.RefreshShownCharacters;
        }

        public override void OnHide()
        {
            this.playerTrackers = new();
            PlayerSystem.PlayerJoined -= this.OnPlayerJoined;
            PlayerSystem.PlayerRemoved -= this.DisconnectPlayer;

            // Unregister player input.
            InputSystem.UIPlayerSubmit -= this.OnSubmit;
            InputSystem.UIPlayerCancel -= this.OnCancel;
            InputSystem.UIPlayerNavigate -= this.OnNavigate;

            ClassManager.CharacterTakenStatusChanged -= this.RefreshShownCharacters;
        }

        private void OnPlayerJoined(Player player)
        {
            this.ConnectPlayer(player);
        }

        #endregion
        #region Navigation Events

        private void OnSubmit(Player player)
        {
            // Ignore input from unjoined players.
            if (player == null || !this.playerTrackers.TryGetValue(player, out PlayerTracker tracker))
                return;

            var playerContainer = this.playerContainers[player.ID];

            switch (tracker.Phase)
            {
                // Proceed to calibration.
                case SelectPhase.Joining:
                    this.ResetCalibration(player);
                    tracker.Phase = SelectPhase.CalibratingInProgress;
                    break;

                // Continue latency calculation process.
                case SelectPhase.CalibratingInProgress:
                    this.Calibrate(player);
                    if (this.IsCalibrationComplete(tracker))
                        tracker.Phase = SelectPhase.CalibratingDone;
                    break;

                // Proceed to character select.
                case SelectPhase.CalibratingDone:
                    tracker.Phase = SelectPhase.CharacterSelection;
                    break;

                // Select character and ready up.
                case SelectPhase.CharacterSelection:
                    if (this.TrySelectCharacter(player))
                        tracker.Phase = SelectPhase.Ready;
                    break;

                // Attempt to start the game if every player is ready.
                case SelectPhase.Ready:
                    this.TryStartGame();
                    break;
            }

            this.ShowPhase(playerContainer, tracker.Phase);
        }

        private void OnCancel(Player player)
        {
            // Ignore input from unjoined players.
            if (player == null || !this.playerTrackers.TryGetValue(player, out PlayerTracker tracker))
                return;

            var playerContainer = this.playerContainers[player.ID];

            switch (tracker.Phase)
            {
                // Quit to start.
                case SelectPhase.Joining:
                    this.TryQuitToStart();
                    break;

                // Disconnect.
                case SelectPhase.CalibratingInProgress:
                    tracker.Phase = SelectPhase.Joining;
                    break;

                // Back to calibrating.
                case SelectPhase.CalibratingDone:
                    this.ResetCalibration(player);
                    tracker.Phase = SelectPhase.CalibratingInProgress;
                    break;

                // Back to calibrating.
                case SelectPhase.CharacterSelection:
                    this.ResetCalibration(player);
                    tracker.Phase = SelectPhase.CalibratingInProgress;
                    break;

                // Deselect character and unready.
                case SelectPhase.Ready:
                    this.DeselectCharacter(player);
                    tracker.Phase = SelectPhase.CharacterSelection;
                    break;
            }

            this.ShowPhase(playerContainer, tracker.Phase);
        }

        private void OnNavigate(Vector2 moveDirection, Player player)
        {
            // Ignore input from unjoined players.
            if (player == null || !this.playerTrackers.TryGetValue(player, out PlayerTracker tracker))
                return;

            // Currently, only character select phase handles movement navigation.
            if (tracker.Phase != SelectPhase.CharacterSelection)
                return;

            VisualElement container = this.playerContainers[player.ID].CharacterSelectionContainer;
            VisualElement characterPicker = container.Q<VisualElement>("c_CharacterPicker");
            VisualElement cosmeticsPicker = container.Q<VisualElement>("c_CosmeticsPicker");

            // Move left or right.
            if (Math.Abs(moveDirection.x) >= Math.Abs(moveDirection.y) && Math.Abs(moveDirection.x) > 0.2f)
            {
                // Select cosmetics.
                if (cosmeticsPicker.ClassListContains("is_focus"))
                {
                    // TODO: Implement //
                    Debug.LogWarning("There are no cosmetics.");
                    return;
                }

                // Select character.
                string currentChar = container.Q<Label>("update_CharacterName").text;
                var shownCharacter = (moveDirection.x > 0)
                    ? ClassManager.GetNextCharacter(currentChar)
                    : ClassManager.GetPreviousCharacter(currentChar);

                this.ChangeShownCharacter(container, shownCharacter);
            }

            // Move up or down.
            else if (Math.Abs(moveDirection.y) > 0.2f)
            {
                // Switch between selecting character or selecting cosmetics.
                characterPicker.ToggleInClassList("is_focus");
                cosmeticsPicker.ToggleInClassList("is_focus");
            }
        }

        public override void OnUpdate()
        {
            // Update calibration blinkers.
            foreach ((var player, var phase) in this.playerTrackers)
            {
                if (phase.Phase == SelectPhase.CalibratingInProgress)
                {
                    var blinker = this.playerContainers[player.ID].CalibrationContainer.Q<BeatIndicator>();
                    blinker.Start();
                    blinker.Update();
                }
            }
        }

        #endregion

        #region Private Functions

        private PlayerContainer GetNewContainer(VisualElement element)
        {
            var phaseContainer = element.Q<VisualElement>("c_Phase");
            var joining = this.joiningTemplate.Instantiate();
            var calibration = this.calibrationTemplate.Instantiate();
            var selection = this.characterSelectionTemplate.Instantiate();
            var ready = this.readyTemplate.Instantiate();

            phaseContainer.Clear();
            phaseContainer.Add(joining);
            phaseContainer.Add(calibration);
            phaseContainer.Add(selection);
            phaseContainer.Add(ready);

            var container = new PlayerContainer()
            {
                Container = element,
                JoiningContainer = joining,
                CalibrationContainer = calibration,
                CharacterSelectionContainer = selection,
                ReadyContainer = ready,
                NavBackHint = element.Q<VisualElement>("c_NavBack"),
                NavNextHint = element.Q<VisualElement>("c_NavNext"),
            };

            this.ResetContainerView(container);
            return container;
        }

        private void ResetContainerView(PlayerContainer container)
        {
            // Reset calibration text.
            container.CalibrationContainer.Q<Label>("update_MaxAttempts").text = this.requiredCalibrationAttempts.ToString();
            container.CalibrationContainer.Q<Label>("update_Counter").text = 0.ToString();

            // Reset shown character.
            this.ChangeShownCharacter(container.CharacterSelectionContainer, ClassManager.GetNextCharacter(""));
            container.CharacterSelectionContainer.Q<VisualElement>("c_CharacterPicker").AddToClassList("is_focus");
            container.CharacterSelectionContainer.Q<VisualElement>("c_CosmeticsPicker").RemoveFromClassList("is_focus");

            // Show default phase.
            this.ShowPhase(container, SelectPhase.Joining);
        }

        private void ShowPhase(PlayerContainer playerContainer, SelectPhase phase)
        {
            playerContainer.JoiningContainer.style.display =
                phase == SelectPhase.Joining
                ? DisplayStyle.Flex : DisplayStyle.None;

            playerContainer.CalibrationContainer.style.display =
                (phase == SelectPhase.CalibratingInProgress || phase == SelectPhase.CalibratingDone)
                ? DisplayStyle.Flex : DisplayStyle.None;
            {
                playerContainer.CalibrationContainer.Q<VisualElement>("phase_Calibrating").style.display =
                    phase == SelectPhase.CalibratingInProgress
                    ? DisplayStyle.Flex : DisplayStyle.None;

                playerContainer.CalibrationContainer.Q<VisualElement>("phase_Results").style.display =
                    phase == SelectPhase.CalibratingDone
                    ? DisplayStyle.Flex : DisplayStyle.None;
            }

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

        private void ChangeShownCharacter(VisualElement characterSelectContainer, ClassManager.CharacterSelectInfo shownClass)
        {
            // Update label.
            characterSelectContainer.Q<Label>("update_CharacterName").text = shownClass.Class.Name;

            // Update portrait.
            VisualElement portrait = characterSelectContainer.Q<VisualElement>("portrait_Character");
            portrait.style.backgroundImage = shownClass.Class.Portrait;

            // Update taken status.
            VisualElement selector = characterSelectContainer.Q<VisualElement>("c_CharacterPicker");
            if (shownClass.IsTaken)
                selector.AddToClassList("taken");
            else
                selector.RemoveFromClassList("taken");
        }

        private void RefreshShownCharacters()
        {
            // Update UI whenever a character is taken.
            foreach (var container in this.playerContainers)
            {
                string currentClassName = container.CharacterSelectionContainer.Q<Label>("update_CharacterName").text;
                var currentCharacter = ClassManager.GetCharacter(currentClassName);
                this.ChangeShownCharacter(container.CharacterSelectionContainer, currentCharacter);
            }
        }

        private bool IsCalibrationComplete(PlayerTracker tracker)
        {
            return tracker.CalibrationAttempts >= this.requiredCalibrationAttempts;
        }

        private void Calibrate(Player player)
        {
            if (!this.playerTrackers.TryGetValue(player, out var tracker) ||
                tracker.Phase != SelectPhase.CalibratingInProgress ||
                this.IsCalibrationComplete(tracker))
                return;

            var calibrationConainer = this.playerContainers[player.ID].CalibrationContainer;
            var attemptCounter = calibrationConainer.Q<Label>("update_Counter");
            var latencyLabel = calibrationConainer.Q<Label>("update_Latency");

            // Add a calibration attempt.
            double latency = BeatSystem.GetLatency(BeatSystem.CurrentTrackTime);
            ScoreSystem.AddInputLatencyForPlayer(player, latency);
            tracker.CalibrationAttempts++;
            attemptCounter.text = tracker.CalibrationAttempts.ToString();
            latencyLabel.text = ((int)(player.Latency * 1000)).ToString();
        }

        private void ResetCalibration(Player player)
        {
            if (!this.playerTrackers.TryGetValue(player, out var tracker))
                return;

            var calibrationConainer = this.playerContainers[player.ID].CalibrationContainer;
            var attemptCounter = calibrationConainer.Q<Label>("update_Counter");
            var latencyLabel = calibrationConainer.Q<Label>("update_Latency");

            attemptCounter.text = "0";
            latencyLabel.text = "0";
            tracker.CalibrationAttempts = 0;
        }

        private bool TrySelectCharacter(Player player)
        {
            var playerContainer = this.playerContainers[player.ID];

            CharacterClass selectedCharacter = ClassManager.SelectCharacter
            (
                player,
                playerContainer.CharacterSelectionContainer.Q<Label>("update_CharacterName").text
            );

            player.SetCharacterClass(selectedCharacter);
            AudioSystem.SetInstrumentActive(player.CharacterClass, true);

            return selectedCharacter != null;
        }

        private void DeselectCharacter(Player player)
        {
            AudioSystem.SetInstrumentActive(player.CharacterClass, false);
            player.SetCharacterClass(null);
            ClassManager.UnselectCharacter(player);
        }

        private bool TryStartGame()
        {
            // Check every player is ready.
            foreach (var phase in this.playerTrackers.Values)
            {
                if (phase.Phase != SelectPhase.Ready)
                    return false;
            }

            // Open team creation UI if this is a new run.
            if (TeamSystem.Team == null)
                this.bandNameSelect.Show();

            // Otherwise, start game.
            else
                GameManager.StartGame();

            this.Hide();
            return true;
        }

        private bool TryQuitToStart()
        {
            // Check every player is disconnected.
            foreach (var phase in this.playerTrackers.Values)
            {
                if (phase.Phase != SelectPhase.Joining)
                    return false;
            }

            // Quit.
            this.startMenu.Show();
            this.Hide();
            return true;
        }

        private void ConnectPlayer(Player player)
        {
            // Create a tracker if the player doesn't already have one.
            if (!this.playerTrackers.ContainsKey(player))
                this.playerTrackers[player] = new PlayerTracker();

            // Reset the tracker.
            var tracker = this.playerTrackers[player];
            tracker.Phase = SelectPhase.CalibratingInProgress;
            tracker.CalibrationAttempts = 0;

            // Advance phase.
            this.ShowPhase(this.playerContainers[player.ID], SelectPhase.CalibratingInProgress);

            // Enable the new player's UI input.
            InputSystem.SwitchInputMapMultiPlayer(InputSystem.InputMap.UI);
        }

        private void DisconnectPlayer(Player player)
        {
            this.ResetContainerView(this.playerContainers[player.ID]);
            this.playerTrackers.Remove(player);
        }

        #endregion
    }
}
