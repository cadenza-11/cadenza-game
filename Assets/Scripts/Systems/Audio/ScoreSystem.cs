using System.Collections.Generic;
using UnityEngine;

namespace Cadenza
{
    /// <summary>
    /// Handles calibration as well as team score and individual player score calculations.
    /// </summary>
    public class ScoreSystem : ApplicationSystem
    {
        public enum HitClassification
        {
            Bad,
            OK,
            Perfect,
        }

        private static ScoreSystem singleton;

        private Dictionary<Player, double> latencyByPlayer;
        private const double ForgivenessMs = 0.04; // 40ms
        private const double LatencyAlpha = 0.50; // how heavily new latency values affect average

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            this.latencyByPlayer = new();
        }

        #region Scoring Methods

        /// <summary>
        /// Returns a value between 0 and 1 correpsonding to how far a player hit from the beat.
        /// </summary>
        /// <param name="timestamp">The time to compare to the last beat</param>
        /// <param name="player">The player to apply the score to, considering their calibration values</param>
        public static float GetScore(double timestamp, Player player = null)
        {
            double offset = GetInputLatencyForPlayer(player) * -1;
            double error = BeatSystem.GetLatency(timestamp + offset);

            return (float)Cadenza.Utils.Math.NormalDist(error, stddev: ForgivenessMs);
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
                ? Cadenza.Utils.Math.EWMA(prevLatencyAvg, latency, LatencyAlpha)
                : latency;

            singleton.latencyByPlayer[player] = newLatencyAvg;
        }

        #endregion
    }
}
