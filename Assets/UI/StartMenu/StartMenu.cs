using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
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
            this.buttonStartGame.clicked += this.OnCharacterSelect;
            this.buttonLastRun = this.root.Q<Button>("b_LastRun");
            Debug.LogWarning("No way to check for last run."); // Check if last run exists in player prefs to Enable and assign to OnLastRun
            this.buttonSettings = this.root.Q<Button>("b_Settings");
            this.buttonSettings.clicked += this.OnSettings;
            this.buttonExit = this.root.Q<Button>("b_Exit");
            this.buttonExit.clicked += this.OnExit;

            this.root.style.display = DisplayStyle.None;
        }

        public override void Show()
        {
            base.Show();

            // Subscribe to any first button press by any player.
            UnityEngine.InputSystem.InputSystem.onAnyButtonPress.CallOnce((control) => this.OnAnyButtonPress(control));

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

        private void OnAnyButtonPress(InputControl control)
        {
            var player = InputSystem.GetPlayerFromDevice(control.device);
            if (player != null)
            {
                PlayerSystem.DisableJoining();
                InputSystem.EnableSinglePlayerInput(player);
            }

            // Swap from Join phase to Options phase display
            this.containerJoin.style.display = DisplayStyle.None;
            this.containerOptions.style.display = DisplayStyle.Flex;
            this.buttonStartGame.Focus();
        }

        private void OnCharacterSelect()
        {
            this.characterSelect.Show();
            this.Hide();
        }

        private void OnSettings()
        {
            // Open settings menu
            Debug.LogWarning("Settings not implemented!");
        }

        private void OnExit()
        {
            // Close game
            Debug.LogWarning("Exit not implemented!");
        }

        #endregion

        #region Private Functions

        // Container updates

        #endregion
    }
}
