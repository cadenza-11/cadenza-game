using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class HUD : UIPanel
    {
        [Header("Team Meter")]
        [Tooltip("How long should it take (in seconds) for the team meter to fill automatically?")]
        [SerializeField] private float durationToFull;

        [Tooltip("How much should each score class fill the meter (in seconds)?")]
        [SerializeField] private Thresholds fillAmount;

        [Tooltip("How much should the combo length speed up the meter?")]
        [SerializeField, Min(1.0f)] private float comboMultiplier;

        private ProgressBar teamMeter;
        private BeatIndicator beatIndicator;
        private Label teamStreak;
        private Dictionary<int, string> streakLabels = new();
        private VisualElement[] healthBars = new VisualElement[4];

        public enum MeterState
        {
            Paused,
            Filling,
            Filled
        }

        public override void OnInitialize()
        {
            this.Hide();

            // Initialize team meter.
            this.teamMeter = this.root.Q<ProgressBar>("meter_TeamMeter");
            this.teamMeter.highValue = this.durationToFull;
            this.teamMeter.lowValue = 0;

            // Initialize team streak.
            this.teamStreak = this.root.Q<Label>("update_TeamStreak");
            ScoreSystem.StreakUpdated += this.OnStreakUpdated;

            // Initialize beat indicator bar.
            this.beatIndicator = this.root.Q<BeatIndicator>();

            // Get health bars.
            int i = 0;
            foreach (VisualElement container in this.root.Query<VisualElement>("c_PlayerHealth").ToList())
            {
                this.healthBars[i] = container;
                i++;
            }
        }

        private void OnStreakUpdated(int streak)
        {
            if (!this.streakLabels.ContainsKey(streak))
                this.streakLabels[streak] = $"x{streak}";

            this.teamStreak.text = this.streakLabels[streak];
        }

        public override void OnGameStart()
        {
            this.Show();

            for (int i = 0; i < 4; i++)
            {
                if (PlayerSystem.TryGetPlayerByID(i, out Player player))
                {
                    this.healthBars[i].style.opacity = 1;
                    this.healthBars[i].Q<Label>("update_CharacterName").text = player.CharacterClass.Name;
                    this.healthBars[i].Q<VisualElement>("portrait_Character").style.backgroundImage = player.CharacterClass.Portrait;
                    ProgressBar health = this.healthBars[i].Q<VisualElement>("c_HealthBar").Q<ProgressBar>("bar");
                    health.highValue = player.Character.GetMaxHealth();
                    player.Character.HealthChanged += (healthValue) => this.OnHealthChanged(healthValue, health);
                }
                else
                    this.healthBars[i].style.opacity = 0;
            }

            this.teamMeter.value = 0;
            this.beatIndicator.Start();

            ScoreSystem.TeamHit += this.OnTeamHit;
            Character.TeamAttackInitiated += this.OnTeamAttackInitiated;
        }

        public override void OnGameStop()
        {
            this.Hide();

            this.teamMeter.value = 0;
            this.beatIndicator.Stop();

            ScoreSystem.TeamHit -= this.OnTeamHit;
            Character.TeamAttackInitiated -= this.OnTeamAttackInitiated;
        }

        public override void OnUpdate()
        {
            if (ApplicationController.State != ApplicationState.GameSession)
                return;

            // Update meter.
            var nextState = this.GetMeterState();
            if (nextState != MeterState.Filled)
                this.FillMeter(Time.deltaTime);

            // Update beat indicator.
            this.beatIndicator.Update();
        }

        private void OnTeamHit(TeamScoreDef def)
        {
            if (this.GetMeterState() == MeterState.Filled)
                return;

            float fillAmount = def.Class switch
            {
                ScoreClass.Bad => 0,
                ScoreClass.OK => this.fillAmount.okScoreMs,
                ScoreClass.Great => this.fillAmount.greatScoreMs,
                ScoreClass.Perfect => this.fillAmount.perfectScoreMs,
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

        #region Health Bar

        private void OnHealthChanged(int health, ProgressBar bar)
        {
            bar.value = health;
        }

        #endregion
    }
}
