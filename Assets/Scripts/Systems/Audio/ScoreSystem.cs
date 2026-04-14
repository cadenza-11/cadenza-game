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
    public struct ScoreWeights
    {

        public float perfectScoreWeight;
        public float greatScoreWeight;
        public float okScoreWeight;
        public float badScoreWeight;
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

        #region Inspector Variables

        [Header("Score Class Thresholds")]
        [SerializeField] private Thresholds individualThresholds;
        [SerializeField] private Thresholds teamThresholds;
        [SerializeField] private ScoreWeights scoreWeights;
        [SerializeField, Min(1)] private float maxScore;

        [Header("Calibration Properties")]
        [SerializeField] private bool enableCalibration;
        [SerializeField] private double latencyAlpha = 0.50;

        [Header("Grade Penalties")]
        [SerializeField, Min(0f)] private float deathPenalty01 = 0.02f;

        #endregion
        #region Public Accessors

        public static Thresholds IndividualThresholds => singleton.individualThresholds;
        public static Results Results => singleton.results;
        public static event Action<ScoreDef> AnyPlayerHit;
        public static event Action<TeamScoreDef> TeamHit;
        public static float MaxScore => singleton.maxScore;

        #endregion

        private readonly StreakManager streakManager = new();
        private TeamScoreDef? teamScoreThisBeat;
        private int[] playerIdScratch;
        private double[] timestampScratch;
        private Dictionary<Player, double> latencyByPlayer;
        private Dictionary<Player, ScoreDef> playerHitsThisBeat;
        private Results results;

        #region Events and Callbacks

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

#if !UNITY_EDITOR
            this.enableCalibration = true;
#endif

            this.latencyByPlayer = new();
            this.playerHitsThisBeat = new();
            SaveSystem.GetPreviousRuns(out Results[] results);

            GameManager.CombatStarted += this.OnCombatStarted;
            GameManager.CombatStopped += this.OnCombatStopped;
        }

        private void OnCombatStarted()
        {
            this.StartTracking();
        }

        private void OnCombatStopped(GameManager.GameResult gameResult)
        {
            this.StopTracking();

            // Save results to file.
            if (gameResult != GameManager.GameResult.Forfeit)
            {
                this.results.Timestamp = DateTimeOffset.UtcNow;

                // Calculate overall score (average of all player scores).
                {
                    int count = this.results.PlayerResults.Count;
                    float sum = 0;
                    foreach (var result in this.results.PlayerResults.Values)
                        sum += result.ScoreTotal;
                    this.results.OverallScore = sum / count;
                }

                // Generate random judge scores that average to the overall score.
                {
                    var generated = GenerateJudgeScores(this.results.OverallScore);
                    this.results.JudgeScores = new float[generated.Count];
                    for (int i = 0; i < generated.Count; i++)
                        this.results.JudgeScores[i] = generated[i];
                }

                SaveSystem.SaveRunToFile(this.results);
            }
        }

        private void StartTracking()
        {
            this.playerHitsThisBeat.Clear();
            this.results = new()
            {
                TeamName = TeamSystem.TeamName,
                LevelName = ApplicationController.CurrentLevel.Name
            };

            // Prepare team hits.
            this.playerIdScratch = new int[PlayerSystem.PlayerCount];
            this.timestampScratch = new double[PlayerSystem.PlayerCount];

            // Prepare streaks.
            this.streakManager.Reset();

            // Listen for events.
            BeatSystem.BeatPlayed += this.OnBeat;

            foreach (var player in PlayerSystem.Players)
            {
                player.PlayerHit += this.OnPlayerHit;
                player.Character.Died += _ => this.results.AddPlayerDeath(player);
            }
        }

        private void StopTracking()
        {
            // Stop listening for events.
            foreach (var player in PlayerSystem.Players)
                player.PlayerHit -= this.OnPlayerHit;

            BeatSystem.BeatPlayed -= this.OnBeat;
        }

        private void OnBeat()
        {
            if (ApplicationController.State != ApplicationState.GameSession)
                return;

            // Reset team streaks.
            if (!this.teamScoreThisBeat.HasValue)
                this.streakManager.ResetTeamStreak();

            // Reset player streaks.
            foreach (var player in PlayerSystem.Players)
            {
                if (!this.playerHitsThisBeat.ContainsKey(player))
                    this.streakManager.ResetPlayerStreak(player);
            }

            // Reset team hit tracking.
            this.playerHitsThisBeat.Clear();
            this.teamScoreThisBeat = null;
        }

        private void OnPlayerHit(ScoreDef def)
        {
            // Register individual hit.
            {
                this.results.AddPlayerScore(def.Player, def.Class);
                this.streakManager.UpdatePlayerStreak(def);
            }

            if (this.teamScoreThisBeat.HasValue)
                return;

            // Register team hit.
            {
                this.playerHitsThisBeat[def.Player] = def;

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
                    this.teamScoreThisBeat = teamScore;

                    this.results.AddTeamScore(teamScore.Class);
                    TeamHit?.Invoke(teamScore);

                    // Handle streak.
                    int teamStreak = this.streakManager.UpdateTeamStreak(teamScore);
                    this.results.AddStreak(teamStreak);
                }
            }

            // Forward "any player hit" event.
            AnyPlayerHit?.Invoke(def);
        }

        private void GetPlayersAndHits(Dictionary<Player, ScoreDef> playerHitsThisBeat, int[] players, double[] timestamps)
        {
            int i = 0;
            foreach ((Player player, ScoreDef score) in playerHitsThisBeat)
            {
                players[i] = player.ID;
                timestamps[i] = score.Timestamp;
                i++;
            }
        }

        #endregion
        #region Scoring

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
        /// Returns the score weighting of a given score class.
        /// </summary>
        public static float GetScoreWeight(ScoreClass scoreClass)
        {
            return scoreClass switch
            {
                ScoreClass.Perfect => singleton.scoreWeights.perfectScoreWeight,
                ScoreClass.Great => singleton.scoreWeights.greatScoreWeight,
                ScoreClass.OK => singleton.scoreWeights.okScoreWeight,
                ScoreClass.Bad => singleton.scoreWeights.badScoreWeight,
                _ => 0
            };
        }

        /// <summary>
        /// Returns a letter grade (S, A, B, C, D, F) for a given score value.
        /// </summary>
        public static string GetGradeLetter(float score)
        {
            float normalized = score / singleton.maxScore;
            if (normalized >= 0.95f) return "S";
            if (normalized >= 0.85f) return "A";
            if (normalized >= 0.75f) return "B";
            if (normalized >= 0.5f) return "C";
            if (normalized >= 0.25f) return "D";
            return "F";
        }

        #endregion
        #region Calibration

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
        /// <param name="latency">The latency value, in seconds</param>
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
        /// Wipes the specified player's stored latencies to prepare for recalibration.
        /// </summary>
        public static void ResetCalibrationDataForPlayer(Player player)
        {
            singleton.latencyByPlayer.Remove(player);
        }

        #endregion
        #region Streaks

        /// <summary>
        /// Subscribe to when a team streak starts, ends, or updates.
        /// </summary>
        public static void RegisterTeamStreakCallbacks(
            StreakManager.TeamStreakCallback onStreakStarted,
            StreakManager.TeamStreakCallback onStreakEnded,
            StreakManager.TeamStreakCallback onStreakUpdated)
        {
            singleton.streakManager.RegisterTeamStreakCallbacks(
                onStreakStarted,
                onStreakEnded,
                onStreakUpdated
            );
        }

        /// <summary>
        /// Subscribe to when a player streak starts, ends, or updates.
        /// </summary>
        public static void RegisterPlayerStreakCallbacks(
            StreakManager.PlayerStreakCallback onStreakStarted,
            StreakManager.PlayerStreakCallback onStreakEnded,
            StreakManager.PlayerStreakCallback onStreakUpdated)
        {
            singleton.streakManager.RegisterPlayerStreakCallbacks(
                onStreakStarted,
                onStreakEnded,
                onStreakUpdated
            );
        }

        #endregion
        #region Grading

        public static float CalculateGrade(ResultsDef results)
        {
            float accuracy = CalculateAccuracy01(
                results.GetCount(ScoreClass.Perfect),
                results.GetCount(ScoreClass.Great),
                results.GetCount(ScoreClass.OK),
                results.GetCount(ScoreClass.Bad)
            );

            float deathPenalty = singleton.maxScore * singleton.deathPenalty01 * results.Deaths;
            float final = (accuracy * singleton.maxScore) - deathPenalty;
            return Mathf.Clamp(final, 0f, singleton.maxScore);
        }

        private static float CalculateAccuracy01(int perfect, int great, int ok, int bad)
        {
            int total = perfect + great + ok + bad;
            if (total == 0) return 0f;

            float perfectW = GetScoreWeight(ScoreClass.Perfect);
            float greatW = GetScoreWeight(ScoreClass.Great);
            float okW = GetScoreWeight(ScoreClass.OK);
            float badW = GetScoreWeight(ScoreClass.Bad);

            float weighted = perfect * perfectW + great * greatW + ok * okW + bad * badW;
            return Mathf.Clamp(weighted / total, 0f, 1f);
        }

        /// <summary>
        /// Generates N discrete positive scores that
        /// average exactly to a desired target value.
        /// </summary>
        /// <param name="targetAverage">Desired average in [0, maxScore]</param>
        /// <param name="judgeCount">N judges</param>
        /// <param name="step">The discrete minimum value that scores can vary</param>
        /// <param name="spread">The maximum range that scores can vary</param>
        public static List<float> GenerateJudgeScores(
            float targetAverage,
            int judgeCount = 3,
            float step = 1f,
            float spread = 20f)
        {
            if (judgeCount <= 0) throw new ArgumentException("judgeCount must be > 0");
            if (step <= 0f) throw new ArgumentException("step must be > 0");

            targetAverage = Mathf.Clamp(targetAverage, 0f, singleton.maxScore);

            int ticksPerPoint = Mathf.RoundToInt(1f / step);
            if (Mathf.Abs(ticksPerPoint * step - 1f) > 0.0001f)
                throw new ArgumentException("step must evenly divide 1.0 (e.g. 0.1, 0.25, 0.5, 1.0)");

            int maxTicks = Mathf.RoundToInt(singleton.maxScore * ticksPerPoint);

            // Snap target average to discrete grid
            int targetAvgTicks = Mathf.RoundToInt(targetAverage * ticksPerPoint);
            long targetTotalTicks = (long)targetAvgTicks * judgeCount;

            int baseTick = targetAvgTicks;
            int spreadTicks = Mathf.Clamp(Mathf.RoundToInt(spread * ticksPerPoint), 0, maxTicks);

            var scores = new int[judgeCount];

            // Initial random offsets (triangular distribution for realism)
            for (int i = 0; i < judgeCount; i++)
            {
                float tri = UnityEngine.Random.value - UnityEngine.Random.value; // [-1,1], peaked at 0
                int offset = Mathf.RoundToInt(tri * spreadTicks);

                scores[i] = Mathf.Clamp(baseTick + offset, 0, maxTicks);
            }

            // Force exact total
            long currentTotal = 0;
            for (int i = 0; i < judgeCount; i++)
                currentTotal += scores[i];

            long delta = targetTotalTicks - currentTotal;
            int direction = delta > 0 ? 1 : -1;
            long remaining = Math.Abs(delta);

            while (remaining > 0)
            {
                int idx = UnityEngine.Random.Range(0, judgeCount);
                int candidate = scores[idx] + direction;

                if (candidate >= 0 && candidate <= maxTicks)
                {
                    scores[idx] = candidate;
                    remaining--;
                }
            }

            var result = new List<float>(judgeCount);
            for (int i = 0; i < judgeCount; i++)
                result.Add(scores[i] / (float)ticksPerPoint);

            return result;
        }
        #endregion
    }
}
