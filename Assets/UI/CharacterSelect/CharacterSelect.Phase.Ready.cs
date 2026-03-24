namespace Cadenza
{
    public partial class CharacterSelect
    {
        private sealed class ReadyPhaseHandler : PhaseHandler
        {
            public ReadyPhaseHandler(CharacterSelect owner)
                : base(owner)
            {
            }

            public override void OnSubmit(Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
                // Try to start once at least one player is ready.
                this.Owner.TryGoToNextPanel();
            }

            public override void OnCancel(Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
                // Back out of ready to revisit settings.
                tracker.Phase = SelectPhase.Settings;
            }
        }

        private bool AllPlayersReady()
        {
            bool hasReady = false;

            foreach (var tracker in this.playerTrackers.Values)
            {
                if (tracker.Phase == SelectPhase.Ready)
                    hasReady = true;

                // Don't start if a joined player is in the
                // middle of calibrating or selecting a character.
                else if (tracker.Phase != SelectPhase.None
                    && tracker.Phase != SelectPhase.Joining)
                    return false;
            }

            // Don't start if no player is Ready.
            return hasReady;
        }

        private bool TryGoToNextPanel()
        {
            if (!this.AllPlayersReady())
                return false;

            this.TransitionTo(this.levelSelectMenu);
            return true;
        }
    }
}
