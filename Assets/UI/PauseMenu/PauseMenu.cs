using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class PauseMenu : UIPanel
    {

        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private SettingsMenu settingsMenu;
        private Button unpause;

        #region System Events
        public override void OnInitialize()
        {
            // Set up UI.
            this.root = (TemplateContainer)this.uiDocument.rootVisualElement;
            this.root.style.display = DisplayStyle.None;

            // Grab elements.
            this.unpause = this.root.Q<Button>("b_Unpause");
            this.unpause.clicked += ApplicationController.UnpauseGame;
            Button settings = this.root.Q<Button>("b_Settings");
            settings.clicked += this.OnSettings;
            Button main = this.root.Q<Button>("b_MainMenu");
            main.clicked += this.OnMainMenu;
            Button exit = this.root.Q<Button>("b_Close");
            exit.clicked += this.OnExit;
            this.Hide();

            ApplicationController.GamePaused += this.OnGamePaused;
            ApplicationController.GameUnpaused += this.OnGameUnpaused;
        }

        public override void Show()
        {
            this.root.style.display = DisplayStyle.Flex;
            this.unpause.Focus();
        }

        public override void Hide()
        {
            this.root.style.display = DisplayStyle.None;
        }

        #endregion

        #region Private Functions

        private void OnGamePaused(Player player)
        {
            this.Show();
        }

        private void OnGameUnpaused()
        {
            this.Hide();
        }

        private void OnSettings()
        {
            this.settingsMenu.Show();
        }

        private void OnMainMenu()
        {
            ApplicationController.ExitToPregame();
            this.Hide();
        }

        private void OnExit()
        {
            ApplicationController.RequestQuit();
        }


        #endregion
    }
}
