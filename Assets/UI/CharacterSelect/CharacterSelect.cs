using System.Collections.Generic;
using Cadenza.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.XInput;
using UnityEngine.UIElements;

namespace Cadenza
{
    /// <summary>
    /// UI screen used for selecting the subset of players that will continue
    /// to the main game, and which unique character they will play as.
    /// </summary>
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
            public VisualElement NamingContainer;
            public OnScreenKeyboard Keyboard;
            public VisualElement ReadyContainer;
            public VisualElement NavBackHint;
            public VisualElement NavNextHint;
        }

        private enum SelectPhase
        {
            None,
            Joining,
            CalibratingInProgress,
            CalibratingDone,
            PlayerNaming,
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
        [SerializeField] private VisualTreeAsset namingTemplate;
        [SerializeField] private VisualTreeAsset readyTemplate;
        [SerializeField] private int requiredCalibrationAttempts;

        #endregion

        #region Variables

        private readonly UIClassManager classManager = new();
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

            InputSystem.PlayerJoined += this.OnPlayerJoined;
            InputSystem.PlayerLeft += this.OnPlayerLeft;
        }

        public override void OnShow()
        {
            AudioSystem.SetState(AudioSystem.State.CharacterSelect);
            InputSystem.EnableJoining();
            this.classManager.ClearCharacterAssignments();

            // Register player roster updates.
            PlayerSystem.PlayerAdded += this.OnPlayerAdded;
            PlayerSystem.PlayerRemoved += this.OnPlayerRemoved;

            // Clear roster.
            foreach (var player in PlayerSystem.Players)
                PlayerSystem.RemovePlayer(player);

            // Register for player input.
            InputSystem.UIPlayerSubmit += this.OnSubmit;
            InputSystem.UIPlayerCancel += this.OnCancel;
            InputSystem.UIPlayerNavigate += this.OnNavigate;
            InputSystem.SwitchInputMapMultiPlayer(InputSystem.InputMap.UI);

            this.classManager.CharacterTakenStatusChanged += this.RefreshShownCharacters;
        }

        public override void OnHide()
        {
            InputSystem.DisableJoining();

            // Unregister player roster updates.
            PlayerSystem.PlayerAdded -= this.OnPlayerAdded;
            PlayerSystem.PlayerRemoved -= this.OnPlayerRemoved;

            // Unregister player input.
            InputSystem.UIPlayerSubmit -= this.OnSubmit;
            InputSystem.UIPlayerCancel -= this.OnCancel;
            InputSystem.UIPlayerNavigate -= this.OnNavigate;

            this.classManager.CharacterTakenStatusChanged -= this.RefreshShownCharacters;
        }

        #endregion
        #region Navigation Events

        private void OnSubmit(Player player)
        {
            if (player == null)
                return;

            if (!this.playerTrackers.ContainsKey(player))
                this.OnPlayerJoined(player);

            var tracker = this.playerTrackers[player];
            var playerContainer = this.playerContainers[player.ID];

            switch (tracker.Phase)
            {
                // Proceed to calibration.
                case SelectPhase.Joining:
                    PlayerSystem.AddPlayer(player);
                    tracker.Phase = SelectPhase.CalibratingInProgress;
                    break;

                // Continue latency calculation process.
                case SelectPhase.CalibratingInProgress:
                    this.Calibrate(player);
                    if (this.IsCalibrationComplete(tracker))
                        tracker.Phase = SelectPhase.CalibratingDone;
                    break;

                // Proceed to player naming.
                case SelectPhase.CalibratingDone:
                    tracker.Phase = SelectPhase.PlayerNaming;
                    break;

                // (This phase has its own phase handling.)
                case SelectPhase.PlayerNaming:
                    playerContainer.Keyboard.OnSubmit();
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
            if (player == null)
                return;

            if (!this.playerTrackers.ContainsKey(player))
                this.OnPlayerJoined(player);

            var tracker = this.playerTrackers[player];
            var playerContainer = this.playerContainers[player.ID];

            switch (tracker.Phase)
            {
                // Quit to start.
                case SelectPhase.Joining:
                    this.TryQuitToStart();
                    break;

                // Disconnect.
                case SelectPhase.CalibratingInProgress:
                    PlayerSystem.RemovePlayer(player);
                    tracker.Phase = SelectPhase.Joining;
                    break;

                // Back to calibrating.
                case SelectPhase.CalibratingDone:
                    this.ResetCalibration(player);
                    tracker.Phase = SelectPhase.CalibratingInProgress;
                    break;

                // (This phase has its own phase handling.)
                case SelectPhase.PlayerNaming:
                    playerContainer.Keyboard.OnCancel();
                    break;

                // Back to calibrating.
                case SelectPhase.CharacterSelection:
                    this.ResetCalibration(player);
                    tracker.Phase = SelectPhase.PlayerNaming;
                    break;

                // Deselect character and unready.
                case SelectPhase.Ready:
                    this.DeselectCharacter(player);
                    tracker.Phase = SelectPhase.CharacterSelection;
                    break;
            }

            this.ShowPhase(playerContainer, tracker.Phase);
        }

        private void OnNavigate(MoveDirection moveDirection, Player player)
        {
            // Ignore input from unjoined players.
            if (player == null || !this.playerTrackers.TryGetValue(player, out PlayerTracker tracker))
                return;

            if (tracker.Phase == SelectPhase.CharacterSelection)
            {
                VisualElement container = this.playerContainers[player.ID].CharacterSelectionContainer;
                VisualElement characterPicker = container.Q<VisualElement>("c_CharacterPicker");
                VisualElement cosmeticsPicker = container.Q<VisualElement>("c_CosmeticsPicker");

                // Move left or right.
                if (moveDirection == MoveDirection.Left || moveDirection == MoveDirection.Right)
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
                    var shownCharacter = moveDirection == MoveDirection.Right
                        ? this.classManager.GetNextCharacter(currentChar)
                        : this.classManager.GetPreviousCharacter(currentChar);

                    this.ChangeShownCharacter(container, shownCharacter);
                }

                // Move up or down.
                else if (moveDirection == MoveDirection.Up || moveDirection == MoveDirection.Down)
                {
                    // Switch between selecting character or selecting cosmetics.
                    characterPicker.ToggleInClassList("is_focus");
                    cosmeticsPicker.ToggleInClassList("is_focus");
                }
            }
            else if (tracker.Phase == SelectPhase.PlayerNaming)
            {
                var keyboard = this.playerContainers[player.ID].Keyboard;
                keyboard.OnNavigate(moveDirection);
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
            var naming = this.namingTemplate.Instantiate();

            phaseContainer.Clear();
            phaseContainer.Add(joining);
            phaseContainer.Add(calibration);
            phaseContainer.Add(naming);
            phaseContainer.Add(selection);
            phaseContainer.Add(ready);

            var container = new PlayerContainer()
            {
                Container = element,
                JoiningContainer = joining,
                CalibrationContainer = calibration,
                CharacterSelectionContainer = selection,
                NamingContainer = naming,
                Keyboard = naming.Q<OnScreenKeyboard>(),
                ReadyContainer = ready,
                NavBackHint = element.Q<VisualElement>("c_NavBack"),
                NavNextHint = element.Q<VisualElement>("c_NavNext"),
            };

            naming.Q<Button>("b_SubmitName").clicked += () => this.OnSubmitName(container);
            naming.Q<Button>("b_CancelName").clicked += () => this.OnCancelName(container);

            this.ShowPhase(container, SelectPhase.None);
            return container;
        }

        private void OnSubmitName(PlayerContainer playerContainer)
        {
            for (int i = 0; i < this.playerContainers.Length; i++)
            {
                if (this.playerContainers[i] == playerContainer &&
                    PlayerSystem.TryGetPlayerByID(i, out Player player))
                {
                    var tracker = this.playerTrackers[player];
                    if (tracker.Phase == SelectPhase.PlayerNaming)
                    {
                        // Set name.
                        player.Name = playerContainer.Keyboard.value;
                        playerContainer.Container.Q<Label>("txt_PlayerName").text = playerContainer.Keyboard.value;

                        // Set phase.
                        this.playerTrackers[player].Phase = SelectPhase.CharacterSelection;
                        this.ShowPhase(playerContainer, tracker.Phase);
                    }
                }
            }
        }

        private void OnCancelName(PlayerContainer playerContainer)
        {
            for (int i = 0; i < this.playerContainers.Length; i++)
            {
                if (this.playerContainers[i] == playerContainer &&
                    PlayerSystem.TryGetPlayerByID(i, out Player player))
                {
                    var tracker = this.playerTrackers[player];
                    if (tracker.Phase == SelectPhase.PlayerNaming)
                    {
                        // Set phase.
                        this.playerTrackers[player].Phase = SelectPhase.CalibratingInProgress;
                        this.ShowPhase(playerContainer, tracker.Phase);
                    }
                }
            }
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

            playerContainer.NamingContainer.style.display =
                phase == SelectPhase.PlayerNaming
                ? DisplayStyle.Flex : DisplayStyle.None;

            playerContainer.NavBackHint.style.opacity =
                (phase == SelectPhase.None || phase == SelectPhase.Joining)
                ? 0 : 1;

            playerContainer.NavNextHint.style.opacity =
                (phase == SelectPhase.None || phase == SelectPhase.Joining || phase == SelectPhase.Ready || phase == SelectPhase.CalibratingInProgress)
                ? 0 : 1;

            if (phase == SelectPhase.Joining)
                playerContainer.Container.RemoveFromClassList("joined");
            else
                playerContainer.Container.AddToClassList("joined");
        }

        private void ChangeShownCharacter(VisualElement characterSelectContainer, UIClassManager.CharacterSelectInfo shownClass)
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
                var currentCharacter = this.classManager.GetCharacter(currentClassName);
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

            var calibrationContainer = this.playerContainers[player.ID].CalibrationContainer;
            var attemptCounter = calibrationContainer.Q<Label>("update_Counter");
            var latencyLabel = calibrationContainer.Q<Label>("update_Latency");

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

            var calibrationContainer = this.playerContainers[player.ID].CalibrationContainer;
            var attemptCounter = calibrationContainer.Q<Label>("update_Counter");
            var latencyLabel = calibrationContainer.Q<Label>("update_Latency");
            var maxAttemptsLabel = calibrationContainer.Q<Label>("update_MaxAttempts");

            attemptCounter.text = "0";
            latencyLabel.text = "0";
            tracker.CalibrationAttempts = 0;
            maxAttemptsLabel.text = this.requiredCalibrationAttempts.ToString();
        }

        private bool TrySelectCharacter(Player player)
        {
            var playerContainer = this.playerContainers[player.ID];

            CharacterClass selectedCharacter = this.classManager.SelectCharacter
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
            this.classManager.UnselectCharacter(player);
        }

        private bool AllPlayersReady()
        {
            bool hasReady = false;

            foreach (var tracker in this.playerTrackers.Values)
            {
                if (tracker.Phase == SelectPhase.Ready)
                    hasReady = true;

                // Don't start if a joined player is in the
                // middle of calibrating or selecting a character.
                else if (tracker.Phase != SelectPhase.None
                    && tracker.Phase != SelectPhase.Joining)
                    return false;
            }

            // Don't start if no player is Ready.
            return hasReady;
        }

        private bool TryStartGame()
        {
            if (!this.AllPlayersReady() || ApplicationController.IsRedirecting)
                return false;

            // Open team creation UI if this is a new run.
            if (TeamSystem.Team == null)
            {
                this.TransitionTo(this.bandNameSelect);
            }

            // Otherwise, start game.
            else
            {
                // Redirect will trigger fade in; don't hide panel until faded in.
                this.Schedule(0.5f, () => this.Hide());
                GameManager.RedirectToBackstage();
            }
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
            this.TransitionTo(this.startMenu);
            return true;
        }

        private void OnPlayerAdded(Player player)
        {
            // Reset the tracker.
            var tracker = this.playerTrackers[player];
            tracker.Phase = SelectPhase.CalibratingInProgress;

            // Reset calibration.
            this.ResetCalibration(player);

            // Reset shown character.
            var container = this.playerContainers[player.ID];
            this.ChangeShownCharacter(container.CharacterSelectionContainer, this.classManager.GetNextCharacter(""));
            container.CharacterSelectionContainer.Q<VisualElement>("c_CharacterPicker").AddToClassList("is_focus");
            container.CharacterSelectionContainer.Q<VisualElement>("c_CosmeticsPicker").RemoveFromClassList("is_focus");

            // Set player name.
            container.Container.Q<Label>("txt_PlayerName").text = player.Name;

            // Advance phase.
            this.ShowPhase(container, SelectPhase.CalibratingInProgress);
        }

        private void OnPlayerRemoved(Player player)
        {
            // Reset the tracker.
            var tracker = this.playerTrackers[player];
            tracker.Phase = SelectPhase.Joining;

            // Reset calibration.
            this.ResetCalibration(player);

            // Reset shown character.
            var container = this.playerContainers[player.ID];
            this.ChangeShownCharacter(container.CharacterSelectionContainer, this.classManager.GetNextCharacter(""));
            container.CharacterSelectionContainer.Q<VisualElement>("c_CharacterPicker").AddToClassList("is_focus");
            container.CharacterSelectionContainer.Q<VisualElement>("c_CosmeticsPicker").RemoveFromClassList("is_focus");

            // Show default phase.
            this.ShowPhase(container, SelectPhase.Joining);
        }

        private void OnPlayerJoined(Player player)
        {
            // Create a tracker for the player.
            if (!this.playerTrackers.ContainsKey(player))
                this.playerTrackers[player] = new PlayerTracker();

            // Show initial phase.
            this.playerTrackers[player].Phase = SelectPhase.Joining;
            this.ShowPhase(this.playerContainers[player.ID], SelectPhase.Joining);

            // Enable the new player's UI input.
            InputSystem.SwitchInputMapMultiPlayer(InputSystem.InputMap.UI);

            // Configure input hints for the player's device.
            var device = player.Input.devices[0];
            var controller = device switch
            {
                Keyboard or Mouse => ControllerType.Keyboard,
                XInputController => ControllerType.Xbox,
                DualShockGamepad => ControllerType.PlayStation,
                _ => ControllerType.All,
            };

            var playerContainerElement = this.playerContainers[player.ID].Container;
            playerContainerElement.UpdateInputHintsInHierarchy(controller);

            // Set player name.
            playerContainerElement.Q<Label>("txt_PlayerName").text = player.Name;

            Debug.Log($"Player {player.ID} joined with controller: {player.Input.devices[0]}");
        }

        private void OnPlayerLeft(Player player)
        {
            // Remove tracker.
            this.playerTrackers.Remove(player);

            // Show disconnected phase.
            this.ShowPhase(this.playerContainers[player.ID], SelectPhase.None);
        }

        #endregion
    }
}
