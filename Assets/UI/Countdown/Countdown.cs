using Cadenza.Utils;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class Countdown : UIPanel
    {
        private static readonly string[] CountdownText = { "3", "2", "1", "GO!" };

        private Label countdownLabel;
        private int countdownIndex;
        private bool isCountdownActive;

        public override void OnInitialize()
        {
            this.countdownLabel = this.root.Q<Label>("txt_Countdown");
            this.Hide();

            GameManager.CombatRequested += this.OnCombatRequested;
            GameManager.CombatStopped += this.OnCombatStopped;
        }

        public override void OnApplicationStop()
        {
            this.StopCountdown();

            GameManager.CombatRequested -= this.OnCombatRequested;
            GameManager.CombatStopped -= this.OnCombatStopped;
        }

        public override void OnGameStop()
        {
            this.StopCountdown();
        }

        private void OnCombatRequested()
        {
            this.countdownIndex = 0;
            this.isCountdownActive = true;
            this.countdownLabel.text = string.Empty;
            this.Show();

            BeatSystem.BeatPlayed -= this.OnBeatPlayed;
            BeatSystem.BeatPlayed += this.OnBeatPlayed;
        }

        private void OnCombatStopped(GameManager.GameResult result)
        {
            this.StopCountdown();
        }

        private void OnBeatPlayed()
        {
            if (!this.isCountdownActive)
                return;

            // Hide on the next beat.
            if (this.countdownIndex >= CountdownText.Length)
            {
                this.StopCountdown();
                return;
            }

            this.countdownLabel.text = CountdownText[this.countdownIndex];
            this.countdownIndex++;

            this.countdownLabel.PunchAndShake(0.2f);
        }

        private void StopCountdown()
        {
            this.isCountdownActive = false;
            this.countdownIndex = 0;

            if (this.countdownLabel != null)
                this.countdownLabel.text = string.Empty;

            BeatSystem.BeatPlayed -= this.OnBeatPlayed;
            this.Hide();
        }
    }
}
