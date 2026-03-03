namespace Cadenza
{
    public partial class CharacterSelect
    {
        private sealed class CalibratingDonePhaseHandler : PhaseHandler
        {
            public CalibratingDonePhaseHandler(CharacterSelect owner)
                : base(owner)
            {
            }

            public override void OnSubmit(Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
                // Continue back to settings after calibration results.
                tracker.Phase = SelectPhase.Settings;
            }

            public override void OnCancel(Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
                // Cancel also returns to settings from results.
                tracker.Phase = SelectPhase.Settings;
            }
        }
    }
}
