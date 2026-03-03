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
        private readonly ResultsDef teamResults = new();

        public string TeamName = string.Empty;
        public string LevelName = string.Empty;
        public DateTime Timestamp;
        public int HighestStreak;
        public float[] JudgeScores = Array.Empty<float>();
        public float OverallScore;

        public IReadOnlyDictionary<PlayerDef, ResultsDef> PlayerResults => this.playerResults;
        public ResultsDef TeamResults => this.teamResults;

        public void AddPlayerScore(Player player, ScoreClass scoreClass)
        {
            var playerDef = new PlayerDef()
            {
                ID = player.ID,
                Name = player.Name,
                ClassID = player.CharacterClass.ID
            };

            this.GetOrCreatePlayerResults(playerDef).AddScore(scoreClass);
        }

        public void AddPlayerScore((string name, int classID) player, ScoreClass scoreClass)
        {
            if (!this.TryGetPlayerDef(player, out var playerDef))
            {
                playerDef = new PlayerDef()
                {
                    ID = this.playerResults.Count,
                    Name = player.name,
                    ClassID = player.classID
                };
            }

            this.GetOrCreatePlayerResults(playerDef).AddScore(scoreClass);
        }

        public void AddPlayerDeath(Player player)
        {
            var playerDef = new PlayerDef()
            {
                ID = player.ID,
                Name = player.Name,
                ClassID = player.CharacterClass.ID
            };

            this.GetOrCreatePlayerResults(playerDef).AddDeath();
        }

        public void AddPlayerDeath((string name, int classID) player)
        {
            if (!this.TryGetPlayerDef(player, out var playerDef))
            {
                playerDef = new PlayerDef()
                {
                    ID = this.playerResults.Count,
                    Name = player.name,
                    ClassID = player.classID
                };
            }

            this.GetOrCreatePlayerResults(playerDef).AddDeath();
        }

        public void AddTeamScore(ScoreClass scoreClass)
        {
            this.teamResults.AddScore(scoreClass);
        }

        public void AddStreak(int streak)
        {
            this.HighestStreak = Mathf.Max(this.HighestStreak, streak);
        }

        private bool TryGetPlayerDef((string name, int classID) player, out PlayerDef playerDef)
        {
            foreach (var key in this.playerResults.Keys)
            {
                if (key.Name == player.name && key.ClassID == player.classID)
                {
                    playerDef = key;
                    return true;
                }
            }

            playerDef = default;
            return false;
        }

        private ResultsDef GetOrCreatePlayerResults(PlayerDef playerDef)
        {
            if (!this.playerResults.ContainsKey(playerDef))
                this.playerResults[playerDef] = new ResultsDef();

            return this.playerResults[playerDef];
        }
    }


    /// <summary>
    /// Encapsulates statistics for a complete run of the game for a team or single player.
    /// </summary>
    public class ResultsDef
    {
        public int Hits { get; private set; }
        public float ScoreTotal { get; private set; }
        public int Deaths { get; private set; }
        private readonly Dictionary<ScoreClass, int> countsByClass;

        public ResultsDef()
        {
            this.Hits = 0;
            this.Deaths = 0;
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
            this.Hits++;
            this.RecalculateScore();
        }

        public void AddDeath()
        {
            this.Deaths++;
            this.RecalculateScore();
        }

        public void RecalculateScore()
        {
            this.ScoreTotal = ScoreSystem.CalculateGrade(this);
        }
    }
}
