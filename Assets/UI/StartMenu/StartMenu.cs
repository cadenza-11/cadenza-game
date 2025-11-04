using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class StartMenu : UIPanel
    {

        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private int TotalCallibrationAttempts;

        private InputAction joinAction;
        private InputAction submitAction;

        private VisualElement containerJoin;
        private VisualElement containerOptions;

        private Button buttonStartGame;
        private Button buttonLastRun;
        private Button buttonSettings;
        private Button buttonExit;

        private int playersReady = 0;

        #region System Events
        public override void OnInitialize()
        {
            // Set up UI.
            this.root = (TemplateContainer)this.uiDocument.rootVisualElement;

            this.containerJoin = this.root.Q<VisualElement>("phase_Join");
            this.containerOptions = this.root.Q<VisualElement>("phase_Select");

            this.buttonStartGame = this.root.Q<Button>("b_StartGame");
            this.buttonStartGame.clicked += this.OnCharacterSelect;
            this.buttonLastRun = this.root.Q<Button>("b_LastRun");
            // Check if last run exists in player prefs to Enable and assign to OnLastRun
            this.buttonSettings = this.root.Q<Button>("b_Settings");
            this.buttonSettings.clicked += this.OnSettings;
            this.buttonExit = this.root.Q<Button>("b_Exit");
            this.buttonExit.clicked += this.OnExit;

            this.submitAction = InputSystem.UIInputMap.Get().FindAction("Submit", throwIfNotFound: true);
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
            // Unsubscribe to action
            // Flex option containers, None join containers
        }

        private void OnCharacterSelect()
        {

        }

        private void OnSettings()
        {

        }

        private void OnExit()
        {

        }
        
        #endregion

        #region Private Functions
        // Container updates
        #endregion
    }
}
