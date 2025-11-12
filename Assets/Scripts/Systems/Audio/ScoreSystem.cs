using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Cadenza
{
    /// <summary>
    /// Handles calibration as well as team score and individual player score calculations.
    /// </summary>
    public class ScoreSystem : ApplicationSystem
    {
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

        private static ScoreSystem singleton;

        [Header("Team Score Properties")]
        [SerializeField] private int teamHitToleranceMs = 200;

        [Header("Score Class Thresholds")]
        [SerializeField] private Thresholds individualThresholds;
        [SerializeField] private Thresholds teamThresholds;

        [Header("Calibration Properties")]
        [SerializeField] private bool enableCalibration;
        [SerializeField] private double latencyAlpha = 0.50;

        public static Thresholds IndividualThresholds => singleton.individualThresholds;

        /// <summary>
        /// Encapsulates data related to a single player "hit" and its accuracy.
        /// </summary>
        public struct ScoreDef
        {
            public readonly double Timestamp;
            public readonly int Beat;
            public readonly double Latency;
            public readonly ScoreClass Class;
            public readonly Player Player;
            public readonly int PlayerID;

            public ScoreDef(double timestamp, double latency, Player player)
            {
                this.Timestamp = timestamp;
                this.Beat = BeatSystem.GetClosestBeat(timestamp);
                this.Latency = latency;
                this.Player = player;
                this.PlayerID = player.ID;
                this.Class = ScoreSystem.GetScoreClass(ScoreSystem.IndividualThresholds, this.Latency);
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

        // Record player hits within a short time window.
        private Dictionary<int, ScoreDef> playerHits;

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

#if !UNITY_EDITOR
            this.enableCalibration = true;
#endif

            this.latencyByPlayer = new();
            this.playerHits = new();
        }

        public override void OnGameStart()
        {
            this.playerHits.Clear();
            PlayerSystem.PlayerHit += this.OnPlayerHit;
        }

        public override void OnBeat()
        {

            this.playerHits.Clear();
        }

        private void OnPlayerHit(ScoreDef def)
        {
            if (PlayerSystem.PlayerCount == 1 || this.playerHits.ContainsKey(def.PlayerID))
                return;

            this.playerHits[def.PlayerID] = def;

            if (PlayerSystem.PlayerCount > 1 && this.playerHits.Count == PlayerSystem.PlayerCount)
            {
                var stddev = Cadenza.Utils.Math.StdDev(this.playerHits.Values.Select(v => v.Timestamp).ToArray());
                var scoreClass = ScoreSystem.GetScoreClass(this.teamThresholds, stddev);
                int soundID = scoreClass switch
                {
                    ScoreClass.Bad => 0,
                    ScoreClass.OK => 0,
                    ScoreClass.Great => 1,
                    ScoreClass.Perfect => 2,
                    _ => 0,
                };

                Debug.Log($"Team accuracy: {this.playerHits.Count} / stddev = {stddev} / class = {scoreClass}");

                if (soundID != 0)
                    AudioSystem.PlayOneShotWithParameter(AudioSystem.PlayerOneShotsEvent, "ID", soundID);

                this.playerHits.Clear();
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
            double offset = singleton.enableCalibration ? GetInputLatencyForPlayer(player) * -1 : 0;
            double error = BeatSystem.GetLatency(timestamp + offset);

            ScoreDef score = new ScoreDef(timestamp + offset, error, player);
            Debug.Log(score);
            return score;
        }

        /// <summary>
        /// Returns a descriptor of a score (e.g. Bad, OK, Perfect)
        /// given the thresholds defined in ScoreSystem.
        /// </summary>
        public static ScoreClass GetScoreClass(Thresholds thresholds, double latency)
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
