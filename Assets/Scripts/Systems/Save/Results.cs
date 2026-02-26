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
        public struct PlayerDef
        {
            public int ID;
            public string Name;
            public int ClassID;

            public override readonly bool Equals(object obj)
            {
                return obj is PlayerDef other && this.ID == other.ID;
            }

            public override readonly int GetHashCode()
            {
                return this.ID.GetHashCode();
            }
        }

        private readonly Dictionary<PlayerDef, ResultsDef> playerResults = new();
        public readonly ResultsDef teamResults = new();
        public string TeamName = string.Empty;
        public string LevelName = string.Empty;
        public DateTime Timestamp;
        public int HighestStreak;

        public IReadOnlyDictionary<PlayerDef, ResultsDef> PlayerResults => this.playerResults;
        public ResultsDef TeamResults => this.teamResults;

        public void AddPlayerScore(Player player, ScoreClass scoreClass)
        {
            this.AddPlayerScore((player.Name, player.CharacterClass.ID), scoreClass);
        }

        public void AddPlayerScore((string name, int classID) player, ScoreClass scoreClass)
        {
            var playerDef = new PlayerDef()
            {
                Name = player.name,
                ClassID = player.classID
            };

            if (!this.playerResults.ContainsKey(playerDef))
                this.playerResults[playerDef] = new ResultsDef();

            this.playerResults[playerDef].AddScore(scoreClass);
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
