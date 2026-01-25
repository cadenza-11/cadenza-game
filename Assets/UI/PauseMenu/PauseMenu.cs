using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class PauseMenu : UIPanel
    {
        [SerializeField] private SettingsMenu settingsMenu;
        protected override VisualElement InitialFocus => this.unpause;
        private Button unpause;
        private Label playerNumber;

        #region System Events
        public override void OnInitialize()
        {
            // Grab elements.
            this.unpause = this.root.Q<Button>("b_Unpause");
            this.unpause.clicked += GameManager.UnpauseGame;
            Button settings = this.root.Q<Button>("b_Settings");
            settings.clicked += this.OnSettings;
            Button main = this.root.Q<Button>("b_MainMenu");
            main.clicked += this.OnMainMenu;
            Button exit = this.root.Q<Button>("b_Close");
            exit.clicked += this.OnExit;
            this.playerNumber = this.root.Q<Label>("update_PlayerNumber");

            this.Hide();

            GameManager.GamePaused += this.OnGamePaused;
            GameManager.GameUnpaused += this.OnGameUnpaused;
        }

        #endregion

        #region Private Functions

        private void OnGamePaused(Player player)
        {
            this.playerNumber.text = (player.ID + 1).ToString();
            this.Show();
        }

        private void OnGameUnpaused()
        {
            this.Hide();
        }

        private void OnSettings()
        {
            this.settingsMenu.previousPanel = this;
            this.settingsMenu.Show();
        }

        private void OnMainMenu()
        {
            GameManager.ExitToPregame();
            this.Hide();
        }

        private void OnExit()
        {
            ApplicationController.RequestQuit();
        }


        #endregion
    }
}
