using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class StartMenu : UIPanel
    {

        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private UIPanel characterSelect;
        [SerializeField] private UIPanel settingsMenu;
        private InputAction joinAction;

        private VisualElement containerJoin;
        private VisualElement containerOptions;

        private Button buttonStartGame;
        private Button buttonLastRun;
        private Button buttonSettings;
        private Button buttonExit;

        private Player playerOne;

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

            this.joinAction = InputSystem.UIInputMap.Get().FindAction("Join", throwIfNotFound: true);
            this.root.style.display = DisplayStyle.None;
        }

        public override void Show()
        {
            base.Show();
            this.joinAction.performed += this.OnPlayerOneJoin;
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

        private void OnPlayerOneJoin(InputAction.CallbackContext context)
        {
            // Set player one
            int id = context.control.device.deviceId;
            PlayerSystem.TryAddPlayer(id, out this.playerOne);
            // Unsubscribe to action
            this.joinAction.performed -= this.OnPlayerOneJoin;
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
