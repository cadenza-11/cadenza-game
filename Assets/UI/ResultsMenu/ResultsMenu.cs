using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class ResultsMenu : UIPanel
    {
        [SerializeField] private FMODUnity.EventReference resultsMusicEvent;
        [SerializeField] private float tweenDuration = 0.5f;

        private readonly ResultsSequence resultsSequence = new();
        private WinLossBanner winLossBanner = new();
        private TeamStats playerTeamStats = new();
        private ActionButtons actionButtons = new();

        private List<PlayerStats> availableContainers = new();
        private Dictionary<Player, PlayerStats> assignedContainers = new();

        private int numPlayersReady;
        private Results currentResults;
        private VisualElement resultsPanel;
        private GameManager.GameResult gameResult;

        private class WinLossBanner
        {
            public Label TeamName;
            public Label EnemyTeamName;
            public Label ResultText;
        }

        private class TeamStats
        {
            public Label TeamName;
            public Label Grade;
            public Label TeamScore;
            public VisualElement MvpPortrait;
            public Label[] ScoreCards;
        }

        private class PlayerStats
        {
            public VisualElement Container;
            public Label PlayerName;
            public Label Instrument;
            public VisualElement CharacterPortrait;
            public Label OverallScore;
            public Label[] Stats;
        }

        private class ActionButtons
        {
            public VisualElement Container;
            public Button NextButton;
            public Button RetryButton;
            public Button LeaderboardButton;
            public Button MainMenuButton;
        }

        #region Application Callbacks

        public override void OnInitialize()
        {
            // Grab references to UI elements.
            this.resultsPanel = this.root.Q<VisualElement>("c_Panel");
            this.actionButtons.Container = this.root.Q<VisualElement>("c_ActionButtons");
            this.actionButtons.NextButton = this.root.Q<Button>("b_Next");
            this.actionButtons.RetryButton = this.root.Q<Button>("b_Retry");
            this.actionButtons.LeaderboardButton = this.root.Q<Button>("b_Leaderboard");
            this.actionButtons.MainMenuButton = this.root.Q<Button>("b_MainMenu");
            this.resultsSequence.OnInitialize(
                this.root,
                this.resultsPanel,
                this.actionButtons.Container,
                this.resultsMusicEvent,
                this.tweenDuration);

            this.actionButtons.NextButton.RegisterCallback<NavigationSubmitEvent>(_ => GameManager.RedirectToBackstage());
            this.actionButtons.RetryButton.RegisterCallback<NavigationSubmitEvent>(_ => GameManager.RestartLevel());
            this.actionButtons.LeaderboardButton.RegisterCallback<NavigationSubmitEvent>(_ => this.ShowLeaderboard());
            this.actionButtons.MainMenuButton.RegisterCallback<NavigationSubmitEvent>(_ => GameManager.ExitToPregame());

            this.winLossBanner.TeamName = this.root.Q<Label>("update_PlayerBand");
            this.winLossBanner.EnemyTeamName = this.root.Q<Label>("update_LevelName");
            this.winLossBanner.ResultText = this.root.Q<Label>("update_WinLoss");

            this.playerTeamStats.TeamName = this.root.Q<Label>("update_PlayerBandBanner");
            this.playerTeamStats.Grade = this.root.Q<Label>("update_LetterGrade");
            this.playerTeamStats.TeamScore = this.root.Q<Label>("update_OverallAccuracy");
            this.playerTeamStats.MvpPortrait = this.root.Q<VisualElement>("portrait_MVP");
            this.playerTeamStats.ScoreCards = this.root.Query<Label>("update_ScoreCard").ToList().ToArray();

            // Initialize player HUD containers.
            foreach (var container in this.root.Query<VisualElement>("c_Player").ToList())
            {
                this.availableContainers.Add(new PlayerStats
                {
                    Container = container,
                    PlayerName = container.Q<Label>("update_PlayerName"),
                    Instrument = container.Q<Label>("update_Instrument"),
                    OverallScore = container.Q<Label>("update_TotalScore"),
                    CharacterPortrait = container.Q<VisualElement>("portrait_Player"),
                    Stats = new Label[]
                    {
                        container.Q<Label>("update_HighestStreak"),
                        container.Q<Label>("update_Perfect"),
                        container.Q<Label>("update_Good"),
                        container.Q<Label>("update_OK"),
                        container.Q<Label>("update_Bad"),
                        container.Q<Label>("update_Deaths"),
                    }
                });
            }

            for (int i = 0; i < this.availableContainers.Count; i++)
                this.availableContainers[i].Container.AddToClassList("no_player");

            GameManager.CombatStopped += this.OnCombatStopped;
            this.Hide();
        }

        public void OnCombatStopped(GameManager.GameResult result)
        {
            if (result == GameManager.GameResult.Forfeit)
                return;

            this.gameResult = result;
            this.currentResults = ScoreSystem.Results;

            // Banner
            this.winLossBanner.ResultText.text = $"{result}!";
            this.winLossBanner.TeamName.text = this.currentResults.TeamName;
            this.winLossBanner.EnemyTeamName.text = this.currentResults.LevelName;

            // Team Stats
            this.playerTeamStats.TeamName.text = TeamSystem.Team.Name;
            this.playerTeamStats.Grade.text = ScoreSystem.GetGradeLetter(this.currentResults.OverallScore);
            this.playerTeamStats.TeamScore.text = this.currentResults.OverallScore.ToString("F2");
            for (int i = 0; i < this.playerTeamStats.ScoreCards.Length; i++)
                this.playerTeamStats.ScoreCards[i].text = this.currentResults.JudgeScores[i].ToString("F0");

            // Player Stats
            (Texture2D mvp, Colorway mvpColorway) = this.PopulatePlayerStats();

            // MVP
            this.playerTeamStats.MvpPortrait.style.opacity = mvp == null ? 0 : 1;
            if (mvp != null)
            {
                this.playerTeamStats.MvpPortrait.schedule.Execute(() =>
                {
                    var styleMaterial = this.playerTeamStats.MvpPortrait.resolvedStyle.unityMaterial.material;
                    var portraitMaterial = Instantiate(styleMaterial);
                    portraitMaterial.SetTexture("_Portrait", mvp);
                    portraitMaterial.SetColor("_PrimaryColor", mvpColorway.PrimaryColor);
                    portraitMaterial.SetColor("_SecondaryColor", mvpColorway.SecondaryColor);
                    portraitMaterial.SetColor("_TertiaryColor", mvpColorway.TertiaryColor);
                    this.playerTeamStats.MvpPortrait.style.unityMaterial = portraitMaterial;
                });
                this.playerTeamStats.MvpPortrait.style.backgroundColor = mvpColorway.SecondaryColor;
            }

            this.Show();
            this.resultsSequence.Play(this.currentResults);
        }

        public override void OnShow()
        {
            this.resultsSequence.OnShow();
            InputSystem.UIPlayerSubmit += this.OnSubmit;
            InputSystem.UIPlayerCancel += this.OnCancel;
            InputSystem.SwitchInputMapMultiPlayer(InputSystem.InputMap.UI);
        }

        public override void OnHide()
        {
            this.resultsSequence.OnHide();

            // Reset player containers.
            foreach (var statsContainer in this.availableContainers)
            {
                statsContainer.Container.AddToClassList("no_player");
                statsContainer.Container.RemoveFromClassList("ready");
                foreach (var stat in statsContainer.Stats)
                    stat.text = "0";
            }

            this.assignedContainers.Clear();
            this.numPlayersReady = 0;

            InputSystem.UIPlayerSubmit -= this.OnSubmit;
            InputSystem.UIPlayerCancel -= this.OnCancel;
        }

        public override void OnGameStart()
        {
            this.resultsSequence.OnGameStart();
        }

        public override void OnApplicationStop()
        {
            this.resultsSequence.OnApplicationStop();
            GameManager.CombatStopped -= this.OnCombatStopped;
        }

        public override void OnGameStop()
        {
            this.resultsSequence.OnGameStop();
            this.Hide();
        }

        #endregion
        #region Navigation Events

        private void OnSubmit(Player player)
        {
            if (this.resultsSequence.IsPlaying)
                return;

            if (player == null)
                return;

            if (!this.assignedContainers.TryGetValue(player, out PlayerStats stats))
                return;

            if (stats.Container.ClassListContains("ready"))
            {
                if (this.numPlayersReady == this.assignedContainers.Count)
                    this.OnAllPlayersReady();
            }
            else
            {
                stats.Container.AddToClassList("ready");
                this.numPlayersReady++;
            }
        }
        private void OnCancel(Player player)
        {
            if (this.resultsSequence.IsPlaying)
                return;

            if (player == null)
                return;

            if (!this.assignedContainers.TryGetValue(player, out PlayerStats stats))
                return;

            if (!stats.Container.ClassListContains("ready"))
                return;

            stats.Container.RemoveFromClassList("ready");
            this.numPlayersReady = Mathf.Max(0, this.numPlayersReady - 1);
        }

        private void OnAllPlayersReady()
        {
            // Show next actions.
            this.actionButtons.Container.style.display = DisplayStyle.Flex;
            this.resultsPanel.style.display = DisplayStyle.None;
            this.actionButtons.LeaderboardButton.style.display = DisplayStyle.Flex;

            if (this.gameResult == GameManager.GameResult.Victory)
            {
                this.actionButtons.NextButton.style.display = DisplayStyle.Flex;
                this.actionButtons.RetryButton.style.display = DisplayStyle.None;
                this.actionButtons.MainMenuButton.style.display = DisplayStyle.Flex;
            }
            else if (this.gameResult == GameManager.GameResult.Loss)
            {
                this.actionButtons.NextButton.style.display = DisplayStyle.None;
                this.actionButtons.RetryButton.style.display = DisplayStyle.Flex;
                this.actionButtons.MainMenuButton.style.display = DisplayStyle.Flex;
            }
        }

        #endregion
        #region Helper Methods

        private (Texture2D, Colorway) PopulatePlayerStats()
        {
            float highestScore = 0f;
            Texture2D highestScorerPortrait = null;
            Colorway highestScorerColorway = null;

            // Assign player stat containers to active players.
            for (int i = 0; i < Mathf.Min(PlayerSystem.Players.Length, this.availableContainers.Count); i++)
            {
                Player player = PlayerSystem.Players[i];
                PlayerStats stats = this.availableContainers[i];
                this.assignedContainers[player] = stats;

                // Initialize player container.
                stats.Container.RemoveFromClassList("no_player");
                stats.PlayerName.text = player.Name;
                stats.Instrument.text = player.CharacterClass.Name;
                stats.CharacterPortrait.style.backgroundColor = player.Colorway.SecondaryColor;
                stats.CharacterPortrait.style.backgroundImage = player.CharacterClass.HeadshotPortrait;
                stats.CharacterPortrait.schedule.Execute(() =>
                {
                    var styleMaterial = stats.CharacterPortrait.resolvedStyle.unityMaterial.material;
                    var portraitMaterial = Instantiate(styleMaterial);
                    portraitMaterial.SetTexture("_Portrait", player.CharacterClass.HeadshotPortrait);
                    portraitMaterial.SetColor("_PrimaryColor", player.Colorway.PrimaryColor);
                    portraitMaterial.SetColor("_SecondaryColor", player.Colorway.SecondaryColor);
                    portraitMaterial.SetColor("_TertiaryColor", player.Colorway.TertiaryColor);
                    stats.CharacterPortrait.style.unityMaterial = portraitMaterial;
                });

                Results.PlayerDef playerDef = new Results.PlayerDef
                {
                    ID = player.ID,
                    Name = player.Name,
                    ClassID = player.CharacterClass.ID,
                };

                if (!this.currentResults.PlayerResults.TryGetValue(playerDef, out ResultsDef playerResults)) continue;

                stats.OverallScore.text = playerResults.ScoreTotal.ToString("F2");
                if (highestScore < playerResults.ScoreTotal)
                {
                    highestScore = playerResults.ScoreTotal;
                    highestScorerPortrait = player.CharacterClass.HeadshotPortrait;
                    highestScorerColorway = player.Colorway;
                }

                // Streak -> Perfect -> Good -> OK -> Bad -> Deaths
                Debug.LogWarning("Streak is temporarily total hits");
                stats.Stats[0].text = playerResults.Hits.ToString(); // Not streak temp
                stats.Stats[1].text = playerResults.GetCount(ScoreClass.Perfect).ToString();
                stats.Stats[2].text = playerResults.GetCount(ScoreClass.Great).ToString();
                stats.Stats[3].text = playerResults.GetCount(ScoreClass.OK).ToString();
                stats.Stats[4].text = playerResults.GetCount(ScoreClass.Bad).ToString();
                stats.Stats[5].text = playerResults.Deaths.ToString();
            }

            return (highestScorerPortrait, highestScorerColorway);
        }

        private void ShowLeaderboard()
        {
            var leaderboard = UISystem.FindPanel<Leaderboard>();
            leaderboard.Show(Leaderboard.ShowMode.FromResults);
            leaderboard.FocusResult(this.currentResults);
        }

        #endregion
    }
}
