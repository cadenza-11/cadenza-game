using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cadenza
{
    /// <summary>
    /// Encapsulates all statistics for a complete run of the game.
    /// </summary>
    public class Results
    {
        private readonly Dictionary<string, ResultsDef> playerResults = new();
        public readonly ResultsDef teamResults = new();
        public string TeamName = string.Empty;
        public DateTime Timestamp;
        public int HighestStreak;

        public IReadOnlyDictionary<string, ResultsDef> PlayerResults => this.playerResults;
        public ResultsDef TeamResults => this.teamResults;

        public void AddPlayerScore(string playerName, ScoreClass scoreClass)
        {
            if (!this.playerResults.ContainsKey(playerName))
                this.playerResults[playerName] = new ResultsDef();

            this.playerResults[playerName].AddScore(scoreClass);
        }

        public void AddTeamScore(ScoreClass scoreClass)
        {
            this.teamResults.AddScore(scoreClass);
        }

        public void AddStreak(int streak)
        {
            this.HighestStreak = Mathf.Max(this.HighestStreak, streak);
        }
    }


    /// <summary>
    /// Encapsulates statistics for a complete run of the game for a team or single player.
    /// </summary>
    public class ResultsDef
    {
        public int Hits { get; private set; }
        public int ScoreTotal { get; private set; }
        private readonly Dictionary<ScoreClass, int> countsByClass;

        public ResultsDef()
        {
            this.Hits = 0;
            this.ScoreTotal = 0;
            this.countsByClass = new();
        }

        public int GetCount(ScoreClass scoreClass)
        {
            return this.countsByClass.GetValueOrDefault(scoreClass);
        }

        public void AddScore(ScoreClass scoreClass)
        {
            if (!this.countsByClass.ContainsKey(scoreClass))
                this.countsByClass[scoreClass] = 0;

            this.countsByClass[scoreClass]++;
            this.ScoreTotal += ScoreSystem.GetScoreValue(scoreClass);
            this.Hits++;
        }
    }
}
