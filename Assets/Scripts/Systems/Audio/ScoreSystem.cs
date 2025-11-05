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

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;
        }

        public override void OnGameStart()
        {
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

        public static double GetInputLatencyForPlayer(Player player)
        {
            singleton.latencyByPlayer ??= new();
            return singleton.latencyByPlayer.GetValueOrDefault(player);
        }

        public static void SetInputLatencyForPlayer(Player player, double latency)
        {
            singleton.latencyByPlayer ??= new();
            singleton.latencyByPlayer[player] = latency;
        }

        #endregion
    }
}
