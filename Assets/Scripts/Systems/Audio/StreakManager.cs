using System.Collections.Generic;

namespace Cadenza
{
    /// <summary>
    /// Manages counting streaks for players and teams. Handles subscription
    /// of events for listeners for streak start, end, and update.
    /// </summary>
    public class StreakManager
    {
        public readonly struct TeamStreakEvent
        {
            public readonly int Value;
            public readonly int PreviousValue;

            public TeamStreakEvent(int value, int previousValue)
            {
                this.Value = value;
                this.PreviousValue = previousValue;
            }
        }

        public readonly struct PlayerStreakEvent
        {
            public readonly Player Player;
            public readonly int Value;
            public readonly int PreviousValue;

            public PlayerStreakEvent(Player player, int value, int previousValue)
            {
                this.Player = player;
                this.Value = value;
                this.PreviousValue = previousValue;
            }
        }

        private class StreakTracker
        {
            public int Current;
        }

        private readonly StreakTracker teamTracker = new();
        private readonly Dictionary<Player, StreakTracker> playerTrackers = new();

        public int TeamStreak => this.teamTracker.Current;

        public delegate void TeamStreakCallback(TeamStreakEvent streakEvent);
        public delegate void PlayerStreakCallback(PlayerStreakEvent streakEvent);

        public TeamStreakCallback TeamStreakStarted;
        public TeamStreakCallback TeamStreakEnded;
        public TeamStreakCallback TeamStreakUpdated;

        public PlayerStreakCallback PlayerStreakStarted;
        public PlayerStreakCallback PlayerStreakEnded;
        public PlayerStreakCallback PlayerStreakUpdated;

        public void RegisterTeamStreakCallbacks(
            TeamStreakCallback onStreakStarted,
            TeamStreakCallback onStreakEnded,
            TeamStreakCallback onStreakUpdated)
        {
            if (onStreakStarted != null)
                this.TeamStreakStarted += onStreakStarted;

            if (onStreakEnded != null)
                this.TeamStreakEnded += onStreakEnded;

            if (onStreakUpdated != null)
                this.TeamStreakUpdated += onStreakUpdated;
        }

        public void RegisterPlayerStreakCallbacks(
            PlayerStreakCallback onStreakStarted,
            PlayerStreakCallback onStreakEnded,
            PlayerStreakCallback onStreakUpdated)
        {
            if (onStreakStarted != null)
                this.PlayerStreakStarted += onStreakStarted;

            if (onStreakEnded != null)
                this.PlayerStreakEnded += onStreakEnded;

            if (onStreakUpdated != null)
                this.PlayerStreakUpdated += onStreakUpdated;
        }

        public int GetPlayerStreak(Player player)
        {
            if (player == null || !this.playerTrackers.TryGetValue(player, out var tracker))
                return 0;

            return tracker.Current;
        }

        public void Reset()
        {
            this.playerTrackers.Clear();

            foreach (var player in PlayerSystem.Players)
            {
                var tracker = new StreakTracker();
                this.playerTrackers[player] = tracker;
                this.SetPlayerStreak(tracker, player, tracker.Current);
            }

            this.SetTeamStreak(this.teamTracker, 0);
        }

        public int UpdatePlayerStreak(ScoreDef def)
        {
            if (def.Player == null)
                return 0;

            if (!this.playerTrackers.TryGetValue(def.Player, out var tracker))
            {
                tracker = new StreakTracker();
                this.playerTrackers[def.Player] = tracker;
                this.SetPlayerStreak(tracker, def.Player, tracker.Current);
            }

            int nextValue = def.Class == ScoreClass.Bad ? 0 : tracker.Current + 1;
            this.SetPlayerStreak(tracker, def.Player, nextValue);
            return tracker.Current;
        }

        public int UpdateTeamStreak(TeamScoreDef def)
        {
            int nextValue = def.Class == ScoreClass.Bad ? 0 : this.teamTracker.Current + 1;
            this.SetTeamStreak(this.teamTracker, nextValue);
            return this.teamTracker.Current;
        }

        public void ResetPlayerStreak(Player player)
        {
            if (this.playerTrackers.TryGetValue(player, out var tracker))
                this.SetPlayerStreak(tracker, player, 0);
        }

        public void ResetTeamStreak()
        {
            this.SetTeamStreak(this.teamTracker, 0);
        }

        private void SetPlayerStreak(StreakTracker tracker, Player player, int nextValue)
        {
            int previousValue = tracker.Current;
            if (previousValue == nextValue)
                return;

            tracker.Current = nextValue;
            var evt = new PlayerStreakEvent(player, nextValue, previousValue);

            if (previousValue == 0 && nextValue > 1)
                this.PlayerStreakStarted?.Invoke(evt);
            else if (previousValue > 0 && nextValue == 0)
                this.PlayerStreakEnded?.Invoke(evt);

            this.PlayerStreakUpdated?.Invoke(evt);
        }

        private void SetTeamStreak(StreakTracker tracker, int nextValue)
        {
            int previousValue = tracker.Current;
            if (previousValue == nextValue)
                return;

            tracker.Current = nextValue;
            var evt = new TeamStreakEvent(nextValue, previousValue);

            if (previousValue == 0 && nextValue > 1)
                this.TeamStreakStarted?.Invoke(evt);
            else if (previousValue > 0 && nextValue == 0)
                this.TeamStreakEnded?.Invoke(evt);

            this.TeamStreakUpdated?.Invoke(evt);
        }
    }
}
