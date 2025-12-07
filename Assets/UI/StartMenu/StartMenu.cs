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
        private bool isInitialized;

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
            this.buttonSettings = this.root.Q<Button>("b_Settings");
            this.buttonExit = this.root.Q<Button>("b_Exit");

            // Configure "continue last run" button.
            this.buttonLastRun.SetEnabled(SaveSystem.SaveFileExists);
            SaveSystem.TeamFileDeleted += () => this.buttonLastRun.SetEnabled(SaveSystem.SaveFileExists);

            this.root.style.display = DisplayStyle.None;
        }

        public override void Show()
        {
            this.containerJoin.style.display = DisplayStyle.Flex;
            this.containerOptions.style.display = DisplayStyle.None;

            // Give single-player input to the first player.
            if (PlayerSystem.PlayerCount < 1)
            {
                PlayerSystem.EnableJoining();
                PlayerSystem.PlayerJoined += this.OnPlayerJoined;
            }
            else
            {
                this.OnPlayerJoined(PlayerSystem.Players[0]);
            }

            this.root.style.display = DisplayStyle.Flex;
            AudioSystem.SetParameter(AudioSystem.Param.LowPass, true);
        }

        public override void Hide()
        {
            base.Hide();
            this.root.style.display = DisplayStyle.None;
            AudioSystem.SetParameter(AudioSystem.Param.LowPass, false);
        }

        public override void OnStart()
        {
            this.Show();
        }

        public override void OnGameStop()
        {
            this.Show();
        }

        public override void OnGameStart()
        {
            this.Hide();
        }

        #endregion
        #region Navigation Events

        private void OnPlayerJoined(Player player)
        {
            // Only get input from the first player to join the game.
            PlayerSystem.PlayerJoined -= this.OnPlayerJoined;

            InputSystem.EnableSinglePlayerInput(player);
            PlayerSystem.DisableJoining();
            AudioSystem.SetParameter(AudioSystem.Param.LowPass, false);

            // Swap from Join phase to Options phase display
            this.containerJoin.style.display = DisplayStyle.None;
            this.containerOptions.style.display = DisplayStyle.Flex;

            if (!this.isInitialized)
            {
                this.buttonStartGame.clicked += this.OnCharacterSelect;
                this.buttonLastRun.clicked += this.OnLastRun;
                this.buttonSettings.clicked += this.OnSettings;
                this.buttonExit.clicked += this.OnExit;
                this.isInitialized = true;
            }

            this.buttonStartGame.Focus();
        }

        private void OnCharacterSelect()
        {
            this.characterSelect.Show();
            this.Hide();
        }

        private void OnLastRun()
        {
            Team team = SaveSystem.GetTeamFromFile();
            if (team == null)
            {
                Debug.LogWarning("Could not read previous save data from file.");
                return;
            }

            TeamSystem.SetTeam(team);
            this.OnCharacterSelect();
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
