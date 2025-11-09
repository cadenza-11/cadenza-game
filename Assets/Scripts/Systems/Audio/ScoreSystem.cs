using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Cadenza
{
    /// <summary>
    /// Handles calibration as well as team score and individual player score calculations.
    /// </summary>
    public class ScoreSystem : ApplicationSystem
    {
        private static ScoreSystem singleton;

        [Header("Score Class Thresholds")]
        [Tooltip("The maximum latency from the beat needed for a perfect score.")]
        [SerializeField] private float perfectScoreMs;

        [Tooltip("The maximum latency from the beat needed for a Great score.")]
        [SerializeField] private float greatScoreMs;

        [Tooltip("The maximum latency from the beat needed for an OK score.")]
        [SerializeField] private float okScoreMs;

        [Header("Calibration Properties")]
        [SerializeField] private bool enableCalibration;
        [SerializeField] private double latencyAlpha = 0.50;

        /// <summary>
        /// Encapsulates data related to a single player "hit" and its accuracy.
        /// </summary>
        public struct ScoreDef
        {
            public readonly double Latency;
            public readonly ScoreClass Class;
            public readonly Player Player;
            public readonly int PlayerID;

            public ScoreDef(double latency, Player player)
            {
                this.Latency = latency;
                this.Player = player;
                this.PlayerID = player.ID;
                this.Class = ScoreSystem.GetScoreClass(this.Latency);
            }

            public override readonly string ToString()
            {
                return $"Player {this.PlayerID} hit: \nLatency: {this.Latency * 1000:f0}ms \nClass: {this.Class}";
            }
        }

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

        private Dictionary<Player, double> latencyByPlayer;

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

#if !UNITY_EDITOR
            this.enableCalibration = true;
#endif

            this.latencyByPlayer = new();
        }

        #region Scoring Methods

        /// <summary>
        /// Returns a value and descriptor of a player's accuracy, given their latency from the beat.
        /// </summary>
        /// <param name="timestamp">The time to compare to the last beat</param>
        /// <param name="player">The player whose calibration values will apply to the score</param>
        public static ScoreDef GetScore(double timestamp, Player player = null)
        {
            double offset = singleton.enableCalibration ? GetInputLatencyForPlayer(player) * -1 : 0;
            double error = BeatSystem.GetLatency(timestamp + offset);

            ScoreDef score = new ScoreDef(error, player);
            Debug.Log(score);
            return score;
        }

        /// <summary>
        /// Returns a descriptor of a score (e.g. Bad, OK, Perfect)
        /// given the thresholds defined in ScoreSystem.
        /// </summary>
        public static ScoreClass GetScoreClass(double latency)
        {
            latency = math.abs(latency);
            if (latency <= singleton.perfectScoreMs / 1000f)
                return ScoreClass.Perfect;
            if (latency <= singleton.greatScoreMs / 1000f)
                return ScoreClass.Great;
            if (latency <= singleton.okScoreMs / 1000f)
                return ScoreClass.OK;
            return ScoreClass.Bad;
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

        #endregion
    }
}
