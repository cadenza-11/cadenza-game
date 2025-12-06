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
            this.buttonSettings = this.root.Q<Button>("b_Settings");
            this.buttonExit = this.root.Q<Button>("b_Exit");

            this.buttonLastRun.SetEnabled(SaveSystem.SaveFileExists);

            this.root.style.display = DisplayStyle.None;
            PlayerSystem.PlayerJoined += this.OnPlayerJoined;
        }

        public override void Show()
        {
            this.containerJoin.style.display = DisplayStyle.Flex;
            this.containerOptions.style.display = DisplayStyle.None;
            PlayerSystem.EnableJoining();
            AudioSystem.SetParameter(AudioSystem.Param.LowPass, true);
            base.Show();
            this.root.style.display = DisplayStyle.Flex;
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

            this.buttonStartGame.clicked += this.OnCharacterSelect;
            this.buttonLastRun.clicked += this.OnLastRun;
            this.buttonSettings.clicked += this.OnSettings;
            this.buttonExit.clicked += this.OnExit;

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
