using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Cadenza
{
    /// <summary>
    /// UI screen used for selecting the subset of players that will continue
    /// to the main game, and which unique character they will play as.
    /// </summary>
    public partial class CharacterSelect : UIPanel
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

        [SerializeField] private VisualTreeAsset joiningTemplate;
        [SerializeField] private VisualTreeAsset settingsTemplate;
        [SerializeField] private VisualTreeAsset hapticsTemplate;
        [SerializeField] private VisualTreeAsset calibrationTemplate;
        [SerializeField] private VisualTreeAsset characterSelectionTemplate;
        [SerializeField] private VisualTreeAsset namingTemplate;
        [SerializeField] private VisualTreeAsset readyTemplate;
        [SerializeField] private int requiredCalibrationAttempts;
        [SerializeField] private Colorway[] colorways;

        #endregion

        #region Variables

        private readonly UIClassManager classManager = new();
        private PlayerContainer[] playerContainers;
        private Dictionary<Player, PlayerTracker> playerTrackers = new();

        private StartMenu startMenu;
        private BandNameSelect bandNameSelect;
        private LevelSelectMenu levelSelectMenu;

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

            // Find other UI panels.
            this.startMenu = UISystem.FindPanel<StartMenu>();
            this.bandNameSelect = UISystem.FindPanel<BandNameSelect>();
            this.levelSelectMenu = UISystem.FindPanel<LevelSelectMenu>();

            this.InitializePhaseHandlers();

            InputSystem.PlayerJoined += this.OnPlayerJoined;
            InputSystem.PlayerLeft += this.OnPlayerLeft;
        }

        public override void OnShow()
        {
            AudioSystem.SetState(AudioSystem.State.CharacterSelect);
            InputSystem.EnableJoining();
            this.classManager.ClearCharacterAssignments();
            this.ResetShownPlayers();

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

            if (!this.playerTrackers.TryGetValue(player, out PlayerTracker tracker))
                return;

            var playerContainer = this.playerContainers[player.ID];

            if (this.phaseHandlers.TryGetValue(tracker.Phase, out var phase))
                phase.OnSubmit(player, tracker, playerContainer);

            this.ShowPhase(playerContainer, tracker.Phase);
        }

        private void OnCancel(Player player)
        {
            if (player == null)
                return;

            if (!this.playerTrackers.ContainsKey(player))
                this.OnPlayerJoined(player);

            if (!this.playerTrackers.TryGetValue(player, out PlayerTracker tracker))
                return;

            var playerContainer = this.playerContainers[player.ID];

            if (this.phaseHandlers.TryGetValue(tracker.Phase, out var phase))
                phase.OnCancel(player, tracker, playerContainer);

            this.ShowPhase(playerContainer, tracker.Phase);
        }

        private void OnNavigate(MoveDirection moveDirection, Player player)
        {
            // Ignore input from unjoined players.
            if (player == null || !this.playerTrackers.TryGetValue(player, out PlayerTracker tracker))
                return;

            if (this.phaseHandlers.TryGetValue(tracker.Phase, out var phase))
                phase.OnNavigate(moveDirection, player, tracker, this.playerContainers[player.ID]);
        }

        public override void OnUpdate()
        {
            foreach ((var player, var tracker) in this.playerTrackers)
                if (this.phaseHandlers.TryGetValue(tracker.Phase, out var phase))
                    phase.OnUpdate(player, tracker, this.playerContainers[player.ID]);
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

            VisualElement cosmeticsPicker = selection.Q<VisualElement>("c_CosmeticsPicker");
            this.ChangeShownColor(cosmeticsPicker, this.colorways[0]);

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

        private void ResetShownPlayers()
        {
            // Clear any stale phase UI from the previous visit.
            for (int i = 0; i < this.playerContainers.Length; i++)
                this.ShowPhase(this.playerContainers[i], SelectPhase.None);

            // Existing connected players do not fire a new join event when reopening
            // the panel, so force their trackers and visuals back to Joining.
            foreach (var player in InputSystem.JoinedPlayersByID.Values)
                this.OnPlayerJoined(player);
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

        #endregion
    }
}
