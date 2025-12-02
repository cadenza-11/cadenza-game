using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class StartMenu : UIPanel
    {

        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private UIPanel characterSelect;
        [SerializeField] private UIPanel settingsMenu;

        private VisualElement containerJoin;
        private VisualElement containerOptions;

        private Button buttonStartGame;
        private Button buttonLastRun;
        private Button buttonSettings;
        private Button buttonExit;

        #region System Events
        public override void OnInitialize()
        {
            // Set up UI.
            this.root = (TemplateContainer)this.uiDocument.rootVisualElement;
            this.root.style.display = DisplayStyle.None;

            this.containerJoin = this.root.Q<VisualElement>("phase_Join");
            this.containerOptions = this.root.Q<VisualElement>("phase_Select");

            this.buttonStartGame = this.root.Q<Button>("b_StartGame");
            this.buttonLastRun = this.root.Q<Button>("b_LastRun");
            Debug.LogWarning("No way to check for last run."); // Check if last run exists in player prefs to Enable and assign to OnLastRun
            this.buttonSettings = this.root.Q<Button>("b_Settings");
            this.buttonSettings.clicked += this.OnSettings;
            this.buttonExit = this.root.Q<Button>("b_Exit");
            this.buttonExit.clicked += this.OnExit;

            this.root.style.display = DisplayStyle.None;
            PlayerSystem.PlayerJoined += this.OnPlayerJoined;
        }

        public override void Show()
        {
            base.Show();
            this.root.style.display = DisplayStyle.Flex;
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

        private void OnPlayerJoined(Player player)
        {
            InputSystem.EnableSinglePlayerInput(player);
            PlayerSystem.DisableJoining();

            // Swap from Join phase to Options phase display
            this.containerJoin.style.display = DisplayStyle.None;
            this.containerOptions.style.display = DisplayStyle.Flex;
            this.buttonStartGame.Focus();
            this.buttonStartGame.clicked += this.OnCharacterSelect;
        }

        private void OnCharacterSelect()
        {
            this.characterSelect.Show();
            this.Hide();
        }

        private void OnSettings()
        {
            this.settingsMenu.Show();
        }

        private void OnExit()
        {
            ApplicationController.RequestQuit();
        }

        #endregion

        #region Private Functions

        // Container updates

        #endregion
    }
}
