using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class StartMenu : UIPanel
    {
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
            this.containerJoin = this.root.Q<VisualElement>("phase_Join");
            this.containerOptions = this.root.Q<VisualElement>("phase_Select");

            this.buttonStartGame = this.root.Q<Button>("b_StartGame");
            this.buttonLastRun = this.root.Q<Button>("b_LastRun");
            this.buttonSettings = this.root.Q<Button>("b_Settings");
            this.buttonExit = this.root.Q<Button>("b_Exit");

            this.buttonStartGame.clicked += this.OnCharacterSelect;
            this.buttonLastRun.clicked += this.OnLastRun;
            this.buttonSettings.clicked += this.OnSettings;
            this.buttonExit.clicked += this.OnExit;

            // Configure "continue last run" button.
            this.buttonLastRun.SetEnabled(SaveSystem.SaveFileExists);
            SaveSystem.TeamFileDeleted += () => this.buttonLastRun.SetEnabled(SaveSystem.SaveFileExists);

            this.Show();
        }

        public override void OnShow()
        {
            this.containerJoin.style.display = DisplayStyle.Flex;
            this.containerOptions.style.display = DisplayStyle.None;

            // No player is connected, wait for a join.
            if (InputSystem.JoinedPlayersByID.Count < 1)
            {
                AudioSystem.SetState(AudioSystem.State.Paused);
                InputSystem.PlayerJoined += this.OnPlayerJoined;
                InputSystem.EnableJoining();
            }
            // Give single-player input to the first player.
            else
            {
                this.ShowMenu(InputSystem.JoinedPlayersByID[0]);
            }
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
            this.ShowMenu(player);
        }

        private void ShowMenu(Player player)
        {
            // Only get input from the first player to join the game.
            InputSystem.PlayerJoined -= this.OnPlayerJoined;

            // Swap from Join phase to Options phase display.
            this.containerJoin.style.display = DisplayStyle.None;
            this.containerOptions.style.display = DisplayStyle.Flex;
            AudioSystem.SetState(AudioSystem.State.Menu);

            // Set single player input.
            InputSystem.SwitchInputMapSinglePlayer(InputSystem.InputMap.UI, player);

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
            this.settingsMenu.previousPanel = this;
            this.settingsMenu.Show();
        }

        private void OnExit()
        {
            ApplicationController.RequestQuit();
        }

        #endregion
    }
}
