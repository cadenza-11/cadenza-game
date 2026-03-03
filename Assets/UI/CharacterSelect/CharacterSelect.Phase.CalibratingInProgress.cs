using UnityEngine.UIElements;

namespace Cadenza
{
    public partial class CharacterSelect
    {
        private sealed class CalibratingInProgressPhaseHandler : PhaseHandler
        {
            public CalibratingInProgressPhaseHandler(CharacterSelect owner)
                : base(owner)
            {
            }

            public override void OnSubmit(Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
                // Record a calibration attempt and advance when done.
                this.Owner.Calibrate(player);
                if (this.Owner.IsCalibrationComplete(tracker))
                    tracker.Phase = SelectPhase.CalibratingDone;
            }

            public override void OnCancel(Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
                // Leave calibration and return to settings.
                tracker.Phase = SelectPhase.Settings;
            }

            public override void OnUpdate(Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
                // Keep beat indicator animating while calibrating.
                var blinker = playerContainer.CalibrationContainer.Q<BeatIndicator>();
                blinker.Start();
                blinker.Update();
            }
        }

        private void BeginCalibration(Player player)
        {
            if (!this.playerTrackers.TryGetValue(player, out PlayerTracker tracker))
                return;

            // Reset calibration counters and enter calibration phase.
            this.ResetCalibration(player);
            tracker.Phase = SelectPhase.CalibratingInProgress;
        }

        private bool IsCalibrationComplete(PlayerTracker tracker)
        {
            return tracker.CalibrationAttempts >= this.requiredCalibrationAttempts;
        }

        private void Calibrate(Player player)
        {
            if (!this.playerTrackers.TryGetValue(player, out var tracker) ||
                tracker.Phase != SelectPhase.CalibratingInProgress ||
                this.IsCalibrationComplete(tracker))
                return;

            var calibrationContainer = this.playerContainers[player.ID].CalibrationContainer;
            var attemptCounter = calibrationContainer.Q<Label>("update_Counter");
            var latencyLabel = calibrationContainer.Q<Label>("update_Latency");

            // Add a calibration attempt.
            double latency = BeatSystem.GetLatency(BeatSystem.CurrentTrackTime);
            ScoreSystem.AddInputLatencyForPlayer(player, latency);
            tracker.CalibrationAttempts++;
            attemptCounter.text = tracker.CalibrationAttempts.ToString();
            latencyLabel.text = ((int)(player.Latency * 1000)).ToString();
        }

        private void ResetCalibration(Player player)
        {
            if (!this.playerTrackers.TryGetValue(player, out var tracker))
                return;

            // Reset calibration UI and tracker values.
            var calibrationContainer = this.playerContainers[player.ID].CalibrationContainer;
            var attemptCounter = calibrationContainer.Q<Label>("update_Counter");
            var latencyLabel = calibrationContainer.Q<Label>("update_Latency");
            var maxAttemptsLabel = calibrationContainer.Q<Label>("update_MaxAttempts");

            attemptCounter.text = "0";
            latencyLabel.text = "0";
            tracker.CalibrationAttempts = 0;
            maxAttemptsLabel.text = this.requiredCalibrationAttempts.ToString();
        }
    }
}
