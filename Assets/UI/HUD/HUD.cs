using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class HUD : UIPanel
    {
        [SerializeField] private UIDocument uiDocument;

        [Header("Team Meter")]
        [Tooltip("How long should it take (in seconds) for the team meter to fill automatically?")]
        [SerializeField] private float durationToFull;

        [Tooltip("How much should each score class fill the meter (in seconds)?")]
        [SerializeField] private ScoreSystem.Thresholds fillAmount;

        [Tooltip("How much should the combo length speed up the meter?")]
        [SerializeField, Min(1.0f)] private float comboMultiplier;

        private Slider teamMeter;

        public enum MeterState
        {
            Paused,
            Filling,
            Filled
        }

        public override void OnInitialize()
        {
            var root = this.uiDocument.rootVisualElement;

            // Initialize team meter.
            this.teamMeter = root.Q<Slider>("team-meter");
            this.teamMeter.highValue = this.durationToFull;
            this.teamMeter.lowValue = 0;
        }

        public override void OnGameStart()
        {
            this.Show();

            this.teamMeter.value = 0;
            this.teamMeter.style.display = DisplayStyle.Flex;

            ScoreSystem.TeamHit += this.OnTeamHit;
            Character.TeamAttackInitiated += this.OnTeamAttackInitiated;
        }

        public override void OnGameStop()
        {
            this.Hide();

            this.teamMeter.value = 0;
            this.teamMeter.style.display = DisplayStyle.None;

            ScoreSystem.TeamHit -= this.OnTeamHit;
            Character.TeamAttackInitiated -= this.OnTeamAttackInitiated;
        }

        public override void OnUpdate()
        {
            var nextState = this.GetMeterState();

            if (nextState != MeterState.Filled)
                this.FillMeter(Time.deltaTime);
        }

        private void OnTeamHit(ScoreSystem.TeamScoreDef def)
        {
            if (this.GetMeterState() == MeterState.Filled)
                return;

            float fillAmount = def.Class switch
            {
                ScoreSystem.ScoreClass.Bad => 0,
                ScoreSystem.ScoreClass.OK => this.fillAmount.okScoreMs,
                ScoreSystem.ScoreClass.Great => this.fillAmount.greatScoreMs,
                ScoreSystem.ScoreClass.Perfect => this.fillAmount.perfectScoreMs,
                _ => 0
            };

            Debug.Log($"Filling meter by {fillAmount} seconds.");

            this.FillMeter(fillAmount);
        }

        private void OnTeamAttackInitiated()
        {
            Debug.Log("Team attack initiated. Zeroing team meter.");
            this.teamMeter.value = 0;
        }

        // Advance the meter by a number of seconds.
        private void FillMeter(float seconds)
        {
            this.teamMeter.value = Mathf.Clamp(this.teamMeter.value + seconds, this.teamMeter.lowValue, this.teamMeter.highValue);
        }

        private MeterState GetMeterState()
        {
            if (this.teamMeter.value >= this.teamMeter.highValue)
                return MeterState.Filled;

            return MeterState.Filling;
        }
    }
}
