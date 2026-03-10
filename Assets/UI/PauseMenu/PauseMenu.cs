using UnityEngine.UIElements;

namespace Cadenza
{
    public class PauseMenu : UIPanel
    {
        protected override VisualElement InitialFocus => this.unpause;
        private SettingsMenu settingsMenu;
        private Button unpause;
        private Label playerName;

        #region System Events
        public override void OnInitialize()
        {
            this.settingsMenu = UISystem.FindPanel<SettingsMenu>();

            // Grab elements.
            this.root.RegisterCallback<NavigationCancelEvent>(evt => GameManager.UnpauseGame(), TrickleDown.TrickleDown);

            this.unpause = this.root.Q<Button>("b_Unpause");
            this.unpause.clicked += GameManager.UnpauseGame;

            Button restart = this.root.Q<Button>("b_Restart");
            restart.clicked += GameManager.RestartLevel;

            Button settings = this.root.Q<Button>("b_Settings");
            settings.clicked += this.OnSettings;

            Button main = this.root.Q<Button>("b_MainMenu");
            main.clicked += this.OnMainMenu;

            Button exit = this.root.Q<Button>("b_Close");
            exit.clicked += this.OnExit;

            this.playerName = this.root.Q<Label>("txt_PlayerName");

            this.Hide();

            GameManager.GamePaused += this.OnGamePaused;
            GameManager.GameUnpaused += this.OnGameUnpaused;
        }

        #endregion

        #region Private Functions

        private void OnGamePaused(Player player)
        {
            this.playerName.text = player.Name;
            this.Show();
        }

        private void OnGameUnpaused()
        {
            this.Hide();
        }

        private void OnSettings()
        {
            this.TransitionTo(this.settingsMenu);
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
