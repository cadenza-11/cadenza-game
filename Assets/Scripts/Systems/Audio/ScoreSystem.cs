using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Cadenza
{
    /// <summary>
    /// A description of a score (e.g. Bad, OK, Perfect) given the thresholds defined in ScoreSystem.
    /// </summary>
    public enum ScoreClass
    {
        Bad,
        OK,
        Great,
        Perfect,
    }

    [Serializable]
    public struct Thresholds
    {
        [Tooltip("The maximum latency from the beat needed for a perfect score.")]
        public float perfectScoreMs;

        [Tooltip("The maximum latency from the beat needed for a Great score.")]
        public float greatScoreMs;

        [Tooltip("The maximum latency from the beat needed for an OK score.")]
        public float okScoreMs;
    }

    [Serializable]
    public struct ScoreValues
    {
        [Tooltip("The discrete score value gained when achieving a perfect hit.")]
        public int perfectScoreValue;

        [Tooltip("The discrete score value gained when achieving a Great hit.")]
        public int greatScoreValue;

        [Tooltip("The discrete score value gained when achieving an OK hit.")]
        public int okScoreValue;

        [Tooltip("The discrete score value gained when achieving a Bad hit.")]
        public int badScoreValue;
    }

    /// <summary>
    /// Encapsulates data related to a single player "hit" and its accuracy.
    /// </summary>
    public readonly struct ScoreDef
    {
        public readonly double Timestamp;
        public readonly int Beat;
        public readonly double Latency;
        public readonly ScoreClass Class;
        public readonly Player Player;
        public readonly int PlayerID;
        public readonly int Value;

        public ScoreDef(double timestamp, double latency, Player player)
        {
            this.Timestamp = timestamp;
            this.Beat = BeatSystem.GetClosestBeat(timestamp);
            this.Latency = latency;
            this.Player = player;
            this.PlayerID = player.ID;
            this.Class = ScoreSystem.GetScoreClass(ScoreSystem.IndividualThresholds, this.Latency);
            this.Value = ScoreSystem.GetScoreValue(this.Class);
        }

        public override readonly string ToString()
        {
            return $"Player {this.PlayerID} hit: \nLatency: {this.Latency * 1000:f0}ms \nClass: {this.Class}";
        }
    }

    /// <summary>
    /// Encapsulates data related to a team player "hit".
    /// </summary>
    public struct TeamScoreDef
    {
        public int[] PlayerIDs;
        public double StdDev;
        public ScoreClass Class;

        public override readonly string ToString()
        {
            return $"(TeamHit) PlayerIDs: {string.Join(',', this.PlayerIDs)} / stddev = {this.StdDev} / class = {this.Class}";
        }
    }

    /// <summary>
    /// Handles calibration as well as team score and individual player score calculations.
    /// </summary>
    public class ScoreSystem : ApplicationSystem
    {
        private static ScoreSystem singleton;

        [Header("Score Class Thresholds")]
        [SerializeField] private Thresholds individualThresholds;
        [SerializeField] private Thresholds teamThresholds;
        [SerializeField] private ScoreValues scoreValues;

        [Header("Calibration Properties")]
        [SerializeField] private bool enableCalibration;
        [SerializeField] private double latencyAlpha = 0.50;

        public static Thresholds IndividualThresholds => singleton.individualThresholds;
        public static event Action<TeamScoreDef> TeamHit;

        private int[] playerIdScratch;
        private double[] timestampScratch;

        private Dictionary<Player, double> latencyByPlayer;
        private Dictionary<int, ScoreDef> playerHitsThisBeat;
        private Results results;

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

#if !UNITY_EDITOR
            this.enableCalibration = true;
#endif

            this.latencyByPlayer = new();
            this.playerHitsThisBeat = new();
        }

        public override void OnGameStart()
        {
            this.playerHitsThisBeat.Clear();
            this.results = new();

            // Prepare team hits.
            this.playerIdScratch = new int[PlayerSystem.PlayerCount];
            this.timestampScratch = new double[PlayerSystem.PlayerCount];

            PlayerSystem.PlayerHit += this.OnPlayerHit;
        }

        public override void OnGameStop()
        {
            // SaveSystem.SaveRunToFile(this.results);
        }

        public override void OnBeat()
        {
            this.playerHitsThisBeat.Clear();
        }

        private void OnPlayerHit(ScoreDef def)
        {
            // Register individual hit.
            {
                this.results.AddPlayerScore(def.PlayerID, def.Class);
            }

            // Register team hit.
            {
                this.playerHitsThisBeat[def.PlayerID] = def;

                // Have all players have hit this beat?
                if (this.playerHitsThisBeat.Count == PlayerSystem.PlayerCount)
                {
                    // Calculate team score.
                    this.GetPlayersAndHits(this.playerHitsThisBeat, this.playerIdScratch, this.timestampScratch);
                    var stddev = Cadenza.Utils.Math.StdDev(this.timestampScratch);
                    var scoreClass = ScoreSystem.GetScoreClass(this.teamThresholds, stddev);

                    // Broadcast team score.
                    var teamScore = new TeamScoreDef()
                    {
                        PlayerIDs = this.playerIdScratch,
                        StdDev = stddev,
                        Class = scoreClass
                    };

                    this.results.AddTeamScore(teamScore.Class);
                    TeamHit?.Invoke(teamScore);

                    this.playerHitsThisBeat.Clear();
                }
            }
        }

        private void GetPlayersAndHits(Dictionary<int, ScoreDef> playerHitsThisBeat, int[] players, double[] timestamps)
        {
            int i = 0;
            foreach ((int id, ScoreDef score) in playerHitsThisBeat)
            {
                players[i] = id;
                timestamps[i] = score.Timestamp;
                i++;
            }
        }


        #region Scoring Methods

        /// <summary>
        /// Returns a value and descriptor of a player's accuracy, given their latency from the beat.
        /// </summary>
        /// <param name="timestamp">The time to compare to the last beat</param>
        /// <param name="player">The player whose calibration values will apply to the score</param>
        public static ScoreDef GetScore(double timestamp, Player player = null)
        {
            double offset = singleton.enableCalibration ? player.Latency * -1 : 0;
            double error = BeatSystem.GetLatency(timestamp + offset);

            ScoreDef score = new ScoreDef(timestamp + offset, error, player);
            Debug.Log(score);
            return score;
        }

        /// <summary>
        /// Returns a descriptor of a score (e.g. Bad, OK, Perfect)
        /// given the thresholds defined in ScoreSystem.
        /// </summary>
        public static ScoreClass GetScoreClass(in Thresholds thresholds, double latency)
        {
            latency = math.abs(latency);
            if (latency <= thresholds.perfectScoreMs / 1000f)
                return ScoreClass.Perfect;
            if (latency <= thresholds.greatScoreMs / 1000f)
                return ScoreClass.Great;
            if (latency <= thresholds.okScoreMs / 1000f)
                return ScoreClass.OK;
            return ScoreClass.Bad;
        }

        /// <summary>
        /// Returns the discrete value of a score.
        /// </summary>
        public static int GetScoreValue(ScoreClass scoreClass)
        {
            return scoreClass switch
            {
                ScoreClass.Perfect => singleton.scoreValues.perfectScoreValue,
                ScoreClass.Great => singleton.scoreValues.greatScoreValue,
                ScoreClass.OK => singleton.scoreValues.okScoreValue,
                ScoreClass.Bad => singleton.scoreValues.badScoreValue,
                _ => 0
            };
        }

        /// <summary>
        /// Returns the average input latency for the player.
        /// </summary>
        public static double GetInputLatencyForPlayer(Player player)
        {
            return player == null ? 0.0 : singleton.latencyByPlayer.GetValueOrDefault(player);
        }

        /// <summary>
        /// Takes the given latency, averages it with the previous
        /// latency values for the given player, and stores the value in player.
        /// </summary>
        public static void AddInputLatencyForPlayer(Player player, double latency)
        {
            // If this is the first data point, use this latency as the mean.
            // Otherwise, calculate the mean.
            double newLatencyAvg =
                singleton.latencyByPlayer.TryGetValue(player, out double prevLatencyAvg)
                ? Cadenza.Utils.Math.EWMA(prevLatencyAvg, latency, singleton.latencyAlpha)
                : latency;

            singleton.latencyByPlayer[player] = newLatencyAvg;
        }

        /// <summary>
        /// Wipes all specified players' stored latencies to prepare for recalibration.
        /// If no players are specified, calibration data will be reset for all players.
        /// </summary>
        /// <param name="players"></param>
        public static void ResetCalibrationDataForPlayers(params Player[] players)
        {
            if (players == null || players.Length == 0)
            {
                singleton.latencyByPlayer.Clear();
                return;
            }

            foreach (var player in players)
                singleton.latencyByPlayer.Remove(player);
        }

        #endregion
    }
}
