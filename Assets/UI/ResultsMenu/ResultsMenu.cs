using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class ResultsMenu : UIPanel
    {
        private WinLossBanner winLossBanner = new();
        private TeamStats playerTeamStats = new();

        private List<PlayerStats> availableContainers = new();
        private Dictionary<Player, PlayerStats> assignedContainers = new();

        private int numPlayersReady;
        private Results currentResults;

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

        #region Application Callbacks

        public override void OnInitialize()
        {
            // Grab references to UI elements.
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

            this.currentResults = ScoreSystem.Results;

            // Banner
            this.winLossBanner.ResultText.text = $"{result}!";
            this.winLossBanner.TeamName.text = this.currentResults.TeamName;
            this.winLossBanner.EnemyTeamName.text = this.currentResults.LevelName;

            // Team Stats
            this.playerTeamStats.TeamName.text = TeamSystem.Team.Name;
            Debug.LogWarning("Grades not calculated yet");
            this.playerTeamStats.Grade.text = "N/A";
            this.playerTeamStats.TeamScore.text = this.currentResults.OverallScore.ToString("F2");
            for (int i = 0; i < this.playerTeamStats.ScoreCards.Length; i++)
                this.playerTeamStats.ScoreCards[i].text = this.currentResults.JudgeScores[i].ToString("F0");

            // Player Stats
            Texture2D mvp = this.PopulatePlayerStats();

            // MVP
            this.playerTeamStats.MvpPortrait.style.opacity = mvp == null ? 0 : 1;
            if (mvp != null) this.playerTeamStats.MvpPortrait.style.backgroundImage = mvp;

            this.Show();
        }

        public override void OnShow()
        {
            InputSystem.UIPlayerSubmit += this.OnSubmit;
            InputSystem.UIPlayerCancel += this.OnCancel;
            InputSystem.SwitchInputMapMultiPlayer(InputSystem.InputMap.UI);
        }

        public override void OnHide()
        {
            // Reset player containers.
            foreach (var statsContainer in this.availableContainers)
            {
                statsContainer.Container.AddToClassList("no_player");
                foreach (var stat in statsContainer.Stats)
                    stat.text = "0";
            }

            this.assignedContainers.Clear();
            this.numPlayersReady = 0;

            InputSystem.UIPlayerSubmit -= this.OnSubmit;
            InputSystem.UIPlayerCancel -= this.OnCancel;
        }

        public override void OnApplicationStop()
        {
            GameManager.CombatStopped -= this.OnCombatStopped;
        }

        #endregion
        #region Navigation Events

        private void OnSubmit(Player player)
        {
            if (player == null)
                return;

            if (!this.assignedContainers.TryGetValue(player, out PlayerStats stats))
                return;

            if (stats.Container.ClassListContains("ready"))
            {
                if (this.numPlayersReady == this.assignedContainers.Count)
                {
                    // FINAL IMPLEMENTATION:
                    // Loss: Restart Level or Pregame
                    // Win: Next Level or Pregame

                    // Temp
                    this.Hide();
                    GameManager.ExitToPregame();
                }
            }
            else
            {
                stats.Container.AddToClassList("ready");
                this.numPlayersReady++;
            }
        }
        private void OnCancel(Player player)
        {
            if (player == null)
                return;

            if (!this.assignedContainers.TryGetValue(player, out PlayerStats stats))
                return;

            if (!stats.Container.ClassListContains("ready"))
                return;

            stats.Container.RemoveFromClassList("ready");
            this.numPlayersReady = Mathf.Max(0, this.numPlayersReady - 1);
        }

        #endregion

        #region Helper Methods

        private Texture2D PopulatePlayerStats()
        {
            float highestScore = 0f;
            Texture2D highestScorerPortrait = null;

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
                stats.CharacterPortrait.style.backgroundImage = player.CharacterClass.Portrait;

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
                    highestScorerPortrait = player.CharacterClass.Portrait;
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

            return highestScorerPortrait;
        }

        #endregion
    }
}
