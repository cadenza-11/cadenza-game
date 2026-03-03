using System.Collections.Generic;
using Cadenza.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Haptics;
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
            public VisualElement SettingsContainer;
            public Button HapticsButton;
            public Button NextButton;
            public Button RecalibrateButton;
            public Button RenameButton;
            public VisualElement[] SettingsItemsWithHaptics;
            public VisualElement[] SettingsItemsWithoutHaptics;
            public VisualElement HapticsContainer;
            public Slider HapticsStrengthSlider;
            public Label HapticsStrengthValue;
            public Slider HapticsDurationSlider;
            public Label HapticsDurationValue;
            public Button HapticsBackButton;
            public VisualElement[] HapticsItems;
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
            Settings,
            Haptics,
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
            public int SettingsFocusIndex;
            public int HapticsFocusIndex;
            public bool SupportsRumbleHaptics;
            public float HapticsStrength;
            public float HapticsDuration;
        }

        #endregion

        #region Serialized Fields

        [SerializeField] private StartMenu startMenu;
        [SerializeField] private BandNameSelect bandNameSelect;
        [SerializeField] private VisualTreeAsset joiningTemplate;
        [SerializeField] private VisualTreeAsset settingsTemplate;
        [SerializeField] private VisualTreeAsset hapticsTemplate;
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
        private const int LastHapticsIndex = 2;
        private const float HapticsStrengthStep = 0.1f;
        private const float HapticsDurationStep = 0.05f;
        private const float DefaultHapticsDuration = 0.10f;

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
                // Proceed to character selection.
                case SelectPhase.Joining:
                    PlayerSystem.AddPlayer(player);
                    tracker.Phase = SelectPhase.CharacterSelection;
                    tracker.SettingsFocusIndex = 0;
                    this.SetSettingsFocus(playerContainer, tracker);
                    break;

                // Select character and proceed to settings.
                case SelectPhase.CharacterSelection:
                    if (this.TrySelectCharacter(player))
                        tracker.Phase = SelectPhase.Settings;
                    break;

                // Open settings actions or continue to ready.
                case SelectPhase.Settings:
                    this.ClampSettingsFocus(playerContainer, tracker);
                    var focusedSetting = this.GetSettingsItems(playerContainer, tracker)[tracker.SettingsFocusIndex];

                    if (focusedSetting == playerContainer.RecalibrateButton)
                    {
                        this.BeginCalibration(player);
                    }
                    else if (focusedSetting == playerContainer.RenameButton)
                    {
                        this.BeginRenaming(player);
                    }
                    else if (focusedSetting == playerContainer.HapticsButton)
                    {
                        tracker.Phase = SelectPhase.Haptics;
                        tracker.HapticsFocusIndex = 0;
                        this.SetHapticsFocus(playerContainer, tracker.HapticsFocusIndex);
                    }
                    else
                    {
                        tracker.Phase = SelectPhase.Ready;
                    }
                    break;

                case SelectPhase.Haptics:
                    if (tracker.HapticsFocusIndex == LastHapticsIndex)
                        tracker.Phase = SelectPhase.Settings;
                    break;

                // Continue latency calculation process.
                case SelectPhase.CalibratingInProgress:
                    this.Calibrate(player);
                    if (this.IsCalibrationComplete(tracker))
                        tracker.Phase = SelectPhase.CalibratingDone;
                    break;

                // Return to settings.
                case SelectPhase.CalibratingDone:
                    tracker.Phase = SelectPhase.Settings;
                    break;

                // (This phase has its own phase handling.)
                case SelectPhase.PlayerNaming:
                    playerContainer.Keyboard.OnSubmit();
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
                case SelectPhase.CharacterSelection:
                    PlayerSystem.RemovePlayer(player);
                    tracker.Phase = SelectPhase.Joining;
                    break;

                // Back to character select.
                case SelectPhase.Settings:
                    this.DeselectCharacter(player);
                    tracker.Phase = SelectPhase.CharacterSelection;
                    break;

                // Back to settings.
                case SelectPhase.Haptics:
                    tracker.Phase = SelectPhase.Settings;
                    break;

                // Back to settings.
                case SelectPhase.CalibratingInProgress:
                    tracker.Phase = SelectPhase.Settings;
                    break;

                // Back to settings.
                case SelectPhase.CalibratingDone:
                    tracker.Phase = SelectPhase.Settings;
                    break;

                // (This phase has its own phase handling.)
                case SelectPhase.PlayerNaming:
                    playerContainer.Keyboard.OnCancel();
                    break;

                // Back to settings.
                case SelectPhase.Ready:
                    tracker.Phase = SelectPhase.Settings;
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

            // Navigate settings.
            else if (tracker.Phase == SelectPhase.Settings)
            {
                int lastSettingsIndex = this.GetSettingsItems(this.playerContainers[player.ID], tracker).Length - 1;

                if (moveDirection == MoveDirection.Up)
                    tracker.SettingsFocusIndex = Mathf.Max(0, tracker.SettingsFocusIndex - 1);
                else if (moveDirection == MoveDirection.Down)
                    tracker.SettingsFocusIndex = Mathf.Min(lastSettingsIndex, tracker.SettingsFocusIndex + 1);

                this.SetSettingsFocus(this.playerContainers[player.ID], tracker);
            }

            // Navigate haptics.
            else if (tracker.Phase == SelectPhase.Haptics)
            {
                if (moveDirection == MoveDirection.Up)
                    tracker.HapticsFocusIndex = Mathf.Max(0, tracker.HapticsFocusIndex - 1);
                else if (moveDirection == MoveDirection.Down)
                    tracker.HapticsFocusIndex = Mathf.Min(LastHapticsIndex, tracker.HapticsFocusIndex + 1);
                else if (moveDirection == MoveDirection.Left && tracker.HapticsFocusIndex == 0)
                    this.SetHapticsStrength(player, tracker.HapticsStrength - HapticsStrengthStep);
                else if (moveDirection == MoveDirection.Right && tracker.HapticsFocusIndex == 0)
                    this.SetHapticsStrength(player, tracker.HapticsStrength + HapticsStrengthStep);
                else if (moveDirection == MoveDirection.Left && tracker.HapticsFocusIndex == 1)
                    this.SetHapticsDuration(player, tracker.HapticsDuration - HapticsDurationStep);
                else if (moveDirection == MoveDirection.Right && tracker.HapticsFocusIndex == 1)
                    this.SetHapticsDuration(player, tracker.HapticsDuration + HapticsDurationStep);

                this.SetHapticsFocus(this.playerContainers[player.ID], tracker.HapticsFocusIndex);
            }

            // Navigate keyboard.
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
            var settings = this.settingsTemplate.Instantiate();
            var haptics = this.hapticsTemplate.Instantiate();
            var calibration = this.calibrationTemplate.Instantiate();
            var selection = this.characterSelectionTemplate.Instantiate();
            var ready = this.readyTemplate.Instantiate();
            var naming = this.namingTemplate.Instantiate();

            phaseContainer.Clear();
            phaseContainer.Add(joining);
            phaseContainer.Add(settings);
            phaseContainer.Add(haptics);
            phaseContainer.Add(calibration);
            phaseContainer.Add(naming);
            phaseContainer.Add(selection);
            phaseContainer.Add(ready);

            var container = new PlayerContainer()
            {
                Container = element,
                JoiningContainer = joining,
                SettingsContainer = settings,
                HapticsButton = settings.Q<Button>("b_Haptics"),
                NextButton = settings.Q<Button>("b_Next"),
                RecalibrateButton = settings.Q<Button>("b_Recalibrate"),
                RenameButton = settings.Q<Button>("b_RenamePlayer"),
                SettingsItemsWithHaptics = new VisualElement[]
                {
                    settings.Q<Button>("b_Recalibrate"),
                    settings.Q<Button>("b_RenamePlayer"),
                    settings.Q<Button>("b_Haptics"),
                    settings.Q<Button>("b_Next")
                },
                SettingsItemsWithoutHaptics = new VisualElement[]
                {
                    settings.Q<Button>("b_Recalibrate"),
                    settings.Q<Button>("b_RenamePlayer"),
                    settings.Q<Button>("b_Next")
                },
                HapticsContainer = haptics,
                HapticsStrengthSlider = haptics.Q<Slider>("slider_HapticsStrength"),
                HapticsStrengthValue = haptics.Q<Label>("txt_HapticsStrengthValue"),
                HapticsDurationSlider = haptics.Q<Slider>("slider_HapticsDuration"),
                HapticsDurationValue = haptics.Q<Label>("txt_HapticsDurationValue"),
                HapticsBackButton = haptics.Q<Button>("b_HapticsBack"),
                HapticsItems = new VisualElement[]
                {
                    haptics.Q<VisualElement>("c_HapticsStrength"),
                    haptics.Q<VisualElement>("c_HapticsDuration"),
                    haptics.Q<Button>("b_HapticsBack")
                },
                CalibrationContainer = calibration,
                CharacterSelectionContainer = selection,
                NamingContainer = naming,
                Keyboard = naming.Q<OnScreenKeyboard>(),
                ReadyContainer = ready,
                NavBackHint = element.Q<VisualElement>("c_NavBack"),
                NavNextHint = element.Q<VisualElement>("c_NavNext"),
            };

            container.Keyboard.SubmitButton.clicked += () => this.OnSubmitName(container);
            container.Keyboard.CancelButton.clicked += () => this.OnCancelName(container);
            container.RecalibrateButton.clicked += () => this.OnPressRecalibrate(container);
            container.RenameButton.clicked += () => this.OnPressRename(container);
            container.HapticsButton.clicked += () => this.OnPressHaptics(container);
            container.NextButton.clicked += () => this.OnPressNext(container);
            container.HapticsBackButton.clicked += () => this.OnPressHapticsBack(container);
            container.HapticsStrengthSlider.RegisterValueChangedCallback(evt => this.OnHapticsStrengthSliderChanged(container, evt.newValue));
            container.HapticsDurationSlider.RegisterValueChangedCallback(evt => this.OnHapticsDurationSliderChanged(container, evt.newValue));
            container.HapticsStrengthSlider.lowValue = 0f;
            container.HapticsStrengthSlider.highValue = 1f;
            container.HapticsStrengthSlider.pageSize = HapticsStrengthStep;
            container.HapticsDurationSlider.lowValue = 0f;
            container.HapticsDurationSlider.highValue = 1f;
            container.HapticsDurationSlider.pageSize = HapticsDurationStep;
            container.HapticsStrengthSlider.SetValueWithoutNotify(1f);
            container.HapticsDurationSlider.SetValueWithoutNotify(DefaultHapticsDuration);
            container.HapticsStrengthValue.text = this.GetHapticsStrengthDisplayText(1f);
            container.HapticsDurationValue.text = this.GetHapticsDurationDisplayText(DefaultHapticsDuration);
            this.SetHapticsFocus(container, 0);

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
                        // Don't allow empty names.
                        var name = playerContainer.Keyboard.value;
                        if (name != string.Empty)
                        {
                            // Set name.
                            player.Name = name;
                            playerContainer.Container.Q<Label>("txt_PlayerName").text = name;
                        }

                        // Set phase.
                        this.playerTrackers[player].Phase = SelectPhase.Settings;
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
                        this.playerTrackers[player].Phase = SelectPhase.Settings;
                        this.ShowPhase(playerContainer, tracker.Phase);
                    }
                }
            }
        }

        private void OnPressRecalibrate(PlayerContainer playerContainer)
        {
            if (!this.TryGetPlayerByContainer(playerContainer, out Player player, out PlayerTracker tracker) ||
                tracker.Phase != SelectPhase.Settings)
                return;

            tracker.SettingsFocusIndex = 0;
            this.SetSettingsFocus(playerContainer, tracker);
            this.BeginCalibration(player);
            this.ShowPhase(playerContainer, tracker.Phase);
        }

        private void OnPressRename(PlayerContainer playerContainer)
        {
            if (!this.TryGetPlayerByContainer(playerContainer, out Player player, out PlayerTracker tracker) ||
                tracker.Phase != SelectPhase.Settings)
                return;

            tracker.SettingsFocusIndex = 1;
            this.SetSettingsFocus(playerContainer, tracker);
            this.BeginRenaming(player);
            this.ShowPhase(playerContainer, tracker.Phase);
        }

        private void OnPressHaptics(PlayerContainer playerContainer)
        {
            if (!this.TryGetPlayerByContainer(playerContainer, out _, out PlayerTracker tracker) ||
                tracker.Phase != SelectPhase.Settings ||
                !tracker.SupportsRumbleHaptics)
                return;

            tracker.SettingsFocusIndex = 2;
            tracker.HapticsFocusIndex = 0;
            this.SetSettingsFocus(playerContainer, tracker);
            this.SetHapticsFocus(playerContainer, tracker.HapticsFocusIndex);
            tracker.Phase = SelectPhase.Haptics;
            this.ShowPhase(playerContainer, tracker.Phase);
        }

        private void OnPressNext(PlayerContainer playerContainer)
        {
            if (!this.TryGetPlayerByContainer(playerContainer, out _, out PlayerTracker tracker) ||
                tracker.Phase != SelectPhase.Settings)
                return;

            tracker.SettingsFocusIndex = this.GetSettingsItems(playerContainer, tracker).Length - 1;
            this.SetSettingsFocus(playerContainer, tracker);
            tracker.Phase = SelectPhase.Ready;
            this.ShowPhase(playerContainer, tracker.Phase);
        }

        private void OnPressHapticsBack(PlayerContainer playerContainer)
        {
            if (!this.TryGetPlayerByContainer(playerContainer, out _, out PlayerTracker tracker) ||
                tracker.Phase != SelectPhase.Haptics)
                return;

            tracker.HapticsFocusIndex = LastHapticsIndex;
            this.SetHapticsFocus(playerContainer, tracker.HapticsFocusIndex);
            tracker.Phase = SelectPhase.Settings;
            this.ShowPhase(playerContainer, tracker.Phase);
        }

        private void OnHapticsStrengthSliderChanged(PlayerContainer playerContainer, float value)
        {
            playerContainer.HapticsStrengthValue.text = this.GetHapticsStrengthDisplayText(value);

            if (!this.TryGetPlayerByContainer(playerContainer, out Player player, out PlayerTracker tracker))
                return;

            tracker.HapticsStrength = value;
            InputSystem.SetHapticsStrengthForPlayer(player, value);
        }

        private void OnHapticsDurationSliderChanged(PlayerContainer playerContainer, float value)
        {
            playerContainer.HapticsDurationValue.text = this.GetHapticsDurationDisplayText(value);

            if (!this.TryGetPlayerByContainer(playerContainer, out Player player, out PlayerTracker tracker))
                return;

            tracker.HapticsDuration = value;
            InputSystem.SetHapticsDurationForPlayer(player, value);
        }

        private bool TryGetPlayerByContainer(PlayerContainer playerContainer, out Player player, out PlayerTracker tracker)
        {
            for (int i = 0; i < this.playerContainers.Length; i++)
            {
                if (this.playerContainers[i] != playerContainer ||
                    !PlayerSystem.TryGetPlayerByID(i, out player) ||
                    !this.playerTrackers.TryGetValue(player, out tracker))
                    continue;

                return true;
            }

            player = null;
            tracker = null;
            return false;
        }

        private void BeginCalibration(Player player)
        {
            if (!this.playerTrackers.TryGetValue(player, out PlayerTracker tracker))
                return;

            this.ResetCalibration(player);
            tracker.Phase = SelectPhase.CalibratingInProgress;
        }

        private void BeginRenaming(Player player)
        {
            if (!this.playerTrackers.TryGetValue(player, out PlayerTracker tracker))
                return;

            tracker.Phase = SelectPhase.PlayerNaming;
            this.playerContainers[player.ID].Keyboard.value = player.Name;
        }

        private void SetHapticsStrength(Player player, float value)
        {
            if (!this.playerTrackers.TryGetValue(player, out PlayerTracker tracker))
                return;

            tracker.HapticsStrength = Mathf.Clamp01(value);

            var container = this.playerContainers[player.ID];
            container.HapticsStrengthSlider.SetValueWithoutNotify(tracker.HapticsStrength);
            container.HapticsStrengthValue.text = this.GetHapticsStrengthDisplayText(tracker.HapticsStrength);
            InputSystem.SetHapticsStrengthForPlayer(player, tracker.HapticsStrength);
        }

        private void SetHapticsDuration(Player player, float value)
        {
            if (!this.playerTrackers.TryGetValue(player, out PlayerTracker tracker))
                return;

            tracker.HapticsDuration = Mathf.Clamp(value, 0f, 1f);

            var container = this.playerContainers[player.ID];
            container.HapticsDurationSlider.SetValueWithoutNotify(tracker.HapticsDuration);
            container.HapticsDurationValue.text = this.GetHapticsDurationDisplayText(tracker.HapticsDuration);
            InputSystem.SetHapticsDurationForPlayer(player, tracker.HapticsDuration);
        }

        private void SetSettingsFocus(PlayerContainer playerContainer, PlayerTracker tracker)
        {
            this.ClampSettingsFocus(playerContainer, tracker);

            foreach (var item in playerContainer.SettingsItemsWithHaptics)
                item.RemoveFromClassList("is_focus");

            var settingsItems = this.GetSettingsItems(playerContainer, tracker);
            settingsItems[tracker.SettingsFocusIndex].AddToClassList("is_focus");
        }

        private VisualElement[] GetSettingsItems(PlayerContainer playerContainer, PlayerTracker tracker)
        {
            return tracker.SupportsRumbleHaptics
                ? playerContainer.SettingsItemsWithHaptics
                : playerContainer.SettingsItemsWithoutHaptics;
        }

        private void ClampSettingsFocus(PlayerContainer playerContainer, PlayerTracker tracker)
        {
            int lastSettingsIndex = this.GetSettingsItems(playerContainer, tracker).Length - 1;
            tracker.SettingsFocusIndex = Mathf.Clamp(tracker.SettingsFocusIndex, 0, lastSettingsIndex);
        }

        private void SetHapticsFocus(PlayerContainer playerContainer, int focusedIndex)
        {
            for (int i = 0; i < playerContainer.HapticsItems.Length; i++)
            {
                if (i == focusedIndex)
                    playerContainer.HapticsItems[i].AddToClassList("is_focus");
                else
                    playerContainer.HapticsItems[i].RemoveFromClassList("is_focus");
            }
        }

        private string GetHapticsStrengthDisplayText(float value)
        {
            return $"{Mathf.RoundToInt(value * 100f)}%";
        }

        private string GetHapticsDurationDisplayText(float value)
        {
            return $"{Mathf.RoundToInt(value * 1000f)}ms";
        }

        private void ShowPhase(PlayerContainer playerContainer, SelectPhase phase)
        {
            playerContainer.JoiningContainer.style.display =
                phase == SelectPhase.Joining
                ? DisplayStyle.Flex : DisplayStyle.None;

            playerContainer.SettingsContainer.style.display =
                phase == SelectPhase.Settings
                ? DisplayStyle.Flex : DisplayStyle.None;

            playerContainer.HapticsContainer.style.display =
                phase == SelectPhase.Haptics
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
                (phase == SelectPhase.None || phase == SelectPhase.Joining || phase == SelectPhase.Ready || phase == SelectPhase.CalibratingInProgress || phase == SelectPhase.Haptics)
                ? 0 : 1;

            if (phase == SelectPhase.Settings &&
                this.TryGetPlayerByContainer(playerContainer, out _, out PlayerTracker tracker))
                this.SetSettingsFocus(playerContainer, tracker);
            if (phase == SelectPhase.Haptics &&
                this.TryGetPlayerByContainer(playerContainer, out _, out PlayerTracker hapticsTracker))
                this.SetHapticsFocus(playerContainer, hapticsTracker.HapticsFocusIndex);

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
            tracker.Phase = SelectPhase.CharacterSelection;
            tracker.SettingsFocusIndex = 0;
            tracker.HapticsFocusIndex = 0;

            // Reset calibration.
            this.ResetCalibration(player);

            // Reset shown character.
            var container = this.playerContainers[player.ID];
            this.ChangeShownCharacter(container.CharacterSelectionContainer, this.classManager.GetNextCharacter(""));
            container.CharacterSelectionContainer.Q<VisualElement>("c_CharacterPicker").AddToClassList("is_focus");
            container.CharacterSelectionContainer.Q<VisualElement>("c_CosmeticsPicker").RemoveFromClassList("is_focus");

            // Set player settings.
            container.Container.Q<Label>("txt_PlayerName").text = player.Name;
            this.UpdateHapticsAvailability(player);
            this.SetHapticsStrength(player, tracker.HapticsStrength);
            this.SetHapticsDuration(player, tracker.HapticsDuration);
            this.SetSettingsFocus(container, tracker);
            this.SetHapticsFocus(container, tracker.HapticsFocusIndex);

            // Advance phase.
            this.ShowPhase(container, SelectPhase.CharacterSelection);
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
                this.playerTrackers[player] = new PlayerTracker()
                {
                    SettingsFocusIndex = 0,
                    HapticsFocusIndex = 0,
                    SupportsRumbleHaptics = false,
                    HapticsStrength = 1f,
                    HapticsDuration = DefaultHapticsDuration
                };

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
            this.UpdateHapticsAvailability(player);

            Debug.Log($"Player {player.ID} joined with controller: {player.Input.devices[0]}");
        }

        private void UpdateHapticsAvailability(Player player)
        {
            if (!this.playerTrackers.TryGetValue(player, out PlayerTracker tracker))
                return;

            var container = this.playerContainers[player.ID];
            tracker.SupportsRumbleHaptics = this.SupportsRumbleHaptics(player);
            container.HapticsButton.style.display =
                tracker.SupportsRumbleHaptics
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            if (!tracker.SupportsRumbleHaptics && tracker.Phase == SelectPhase.Haptics)
                tracker.Phase = SelectPhase.Settings;

            this.ClampSettingsFocus(container, tracker);
        }

        private bool SupportsRumbleHaptics(Player player)
        {
            foreach (var device in player.Input.devices)
            {
                if (device is IDualMotorRumble)
                    return true;
            }

            return false;
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
