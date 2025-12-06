using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class PauseMenu : UIPanel
    {

        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private SettingsMenu settingsMenu;
        [SerializeField] private StartMenu startMenu;


        #region System Events
        public override void OnInitialize()
        {
            // Set up UI.
            this.root = (TemplateContainer)this.uiDocument.rootVisualElement;
            this.root.style.display = DisplayStyle.None;

            // Grab elements.
            Button unpause = this.root.Q<Button>("b_Unpause");
            unpause.clicked += this.Hide;
            Button settings = this.root.Q<Button>("b_Settings");
            settings.clicked += this.OnSettings;
            Button main = this.root.Q<Button>("b_MainMenu");
            main.clicked += this.OnMainMenu;
            Button exit = this.root.Q<Button>("b_Close");
            exit.clicked += this.OnExit;
            this.Hide();
        }

        public override void Show()
        {
            base.Show();
            PlayerSystem.TryGetPlayerByID(0, out Player player);
            if (player != null)
                InputSystem.EnableSinglePlayerInput(player);
            this.root.style.display = DisplayStyle.Flex;
        }

        public override void Hide()
        {
            base.Hide();
            this.root.style.display = DisplayStyle.None;
        }

        #endregion

        #region Private Functions

        private void OnSettings()
        {
            this.settingsMenu.Show();
        }

        private void OnMainMenu()
        {
            Debug.LogWarning("Not exiting level (no implementation)");
            this.startMenu.Show();
            this.Hide();
        }

        private void OnExit()
        {
            ApplicationController.RequestQuit();
        }


        #endregion
    }
}
