using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class StartMenu : UIPanel
    {
        private UIPanel characterSelect;
        private UIPanel settingsMenu;
        private UIPanel leaderboardMenu;

        private VisualElement containerJoin;
        private VisualElement containerOptions;

        private Button buttonStartGame;
        private Button buttonLastRun;
        private Button buttonLeaderboard;
        private Button buttonSettings;
        private Button buttonExit;

        #region System Events
        public override void OnInitialize()
        {
            this.characterSelect = UISystem.FindPanel<CharacterSelect>();
            this.settingsMenu = UISystem.FindPanel<SettingsMenu>();
            this.leaderboardMenu = UISystem.FindPanel<Leaderboard>();

            this.containerJoin = this.root.Q<VisualElement>("phase_Join");
            this.containerOptions = this.root.Q<VisualElement>("phase_Select");

            this.buttonStartGame = this.root.Q<Button>("b_StartGame");
            this.buttonLastRun = this.root.Q<Button>("b_LastRun");
            this.buttonLeaderboard = this.root.Q<Button>("b_Leaderboard");
            this.buttonSettings = this.root.Q<Button>("b_Settings");
            this.buttonExit = this.root.Q<Button>("b_Exit");

            this.buttonStartGame.clicked += this.OnNewRun;
            this.buttonLastRun.clicked += this.OnLastRun;
            this.buttonLeaderboard.clicked += this.OnLeaderboard;
            this.buttonSettings.clicked += this.OnSettings;
            this.buttonExit.clicked += this.OnExit;

            // Configure "continue last run" button.
            this.buttonLastRun.SetEnabled(SaveSystem.SaveFileExists);
            SaveSystem.TeamFileDeleted += () => this.buttonLastRun.SetEnabled(SaveSystem.SaveFileExists);
            SaveSystem.TeamFileCreated += () => this.buttonLastRun.SetEnabled(SaveSystem.SaveFileExists);

            // Configure "view leaderboard" button.
            this.buttonLeaderboard.SetEnabled(SaveSystem.ResultsFileExists);
            SaveSystem.ResultsFileDeleted += () => this.buttonLeaderboard.SetEnabled(SaveSystem.ResultsFileExists);
            SaveSystem.ResultsFileCreated += () => this.buttonLeaderboard.SetEnabled(SaveSystem.ResultsFileExists);

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
            if (ApplicationController.RequestedLevel == null)
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
            AudioSystem.PlayOneShot(Sound.UI.InitialJoin);
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

        private void OnNewRun()
        {
            SaveSystem.DeleteTeamFile();
            this.TransitionTo(this.characterSelect);
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
            this.TransitionTo(this.characterSelect);
        }

        private void OnSettings()
        {
            this.TransitionTo(this.settingsMenu);
        }

        private void OnLeaderboard()
        {
            if (this.leaderboardMenu == null)
            {
                Debug.LogWarning("Leaderboard panel not found under UISystem.");
                return;
            }

            this.TransitionTo(this.leaderboardMenu);
        }

        private void OnExit()
        {
            ApplicationController.RequestQuit();
        }

        #endregion
    }
}
