using System.Collections.Generic;
using DG.Tweening;
using Unity.Collections;
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
        private List<VisualElement> availableContainers = new();
        private Dictionary<Player, VisualElement> assignedContainers = new();

        public enum MeterState
        {
            Paused,
            Filling,
            Filled
        }

        #region Application Callbacks

        public override void OnInitialize()
        {
            this.Hide();

            // Initialize team meter.
            this.teamMeter = this.root.Q<ProgressBar>("meter_TeamMeter");
            this.teamMeter.highValue = this.durationToFull;
            this.teamMeter.lowValue = 0;

            // Initialize team streak.
            this.teamStreak = this.root.Q<Label>("update_TeamStreak");
            ScoreSystem.RegisterTeamStreakCallbacks(this.OnTeamStreakStarted, this.OnTeamStreakEnded, this.OnTeamStreakUpdated);
            ScoreSystem.RegisterPlayerStreakCallbacks(this.OnPlayerStreakStarted, this.OnPlayerStreakEnded, this.OnPlayerStreakUpdated);

            // Initialize beat indicator bar.
            this.beatIndicator = this.root.Q<BeatIndicator>();

            // Initialize player HUD containers.
            this.availableContainers = this.root.Query<VisualElement>("c_PlayerHealth").ToList();

            for (int i = 0; i < this.availableContainers.Count; i++)
                this.availableContainers[i].style.display = DisplayStyle.None;
        }

        public override void OnGameStart()
        {
            // Assign player HUD containers to active players.
            for (int i = 0; i < Mathf.Min(PlayerSystem.Players.Length, this.availableContainers.Count); i++)
            {
                Player player = PlayerSystem.Players[i];
                VisualElement container = this.availableContainers[i];
                this.assignedContainers[player] = container;

                // Initialize player container.
                container.style.display = DisplayStyle.Flex;
                container.Q<Label>("update_CharacterName").text = $"{player.Name} ({player.CharacterClass.Name})";
                container.Q<VisualElement>("portrait_Character").style.backgroundImage = player.CharacterClass.Portrait;

                // Initialize health.
                ProgressBar health = container.Q<VisualElement>("c_HealthBar").Q<ProgressBar>("bar");
                health.highValue = player.Character.MaxHealth;
                player.Character.HealthChanged += (healthValue, isFainted) => {
                    this.OnHealthChanged(healthValue, health);
                    if (isFainted) container.AddToClassList("fainted");
                    else container.RemoveFromClassList("fainted");
                };

                // Initialize flow.
                ProgressBar flow = container.Q<VisualElement>("c_FlowBar").Q<ProgressBar>("bar");
                flow.highValue = player.Character.FlowThreshold;
                player.Character.FlowChanged += (flowValue) => this.OnFlowChanged(flowValue, flow);

                // Initialize accuracy.
                player.PlayerHit += this.OnPlayerHit;
            }

            this.teamMeter.value = 0;
            this.beatIndicator.Start();

            ScoreSystem.TeamHit += this.OnTeamHit;
            Character.TeamAttackInitiated += this.OnTeamAttackInitiated;

            this.Show();
        }

        public override void OnGameStop()
        {
            this.Hide();

            // Reset player containers.
            foreach ((var player, var container) in this.assignedContainers)
            {
                player.PlayerHit -= this.OnPlayerHit;
                container.style.display = DisplayStyle.None;
            }

            this.assignedContainers.Clear();

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

        #endregion
        #region Team Combo Meter

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

            this.FillMeter(fillAmount);
        }

        private void OnTeamAttackInitiated()
        {
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

        #endregion
        #region Health Bar

        private void OnHealthChanged(float health, ProgressBar bar)
        {
            bar.value = health;
        }

        #endregion
        #region Flow Bar

        private void OnFlowChanged(float flow, ProgressBar bar)
        {
            bar.value = Mathf.Min(flow, bar.highValue);
        }

        #endregion
        #region Accuracy

        private void OnPlayerHit(ScoreDef def)
        {
            if (!this.assignedContainers.TryGetValue(def.Player, out VisualElement container))
                return;

            VisualElement accuracy = container.Q<VisualElement>("c_Accuracy");

            Label accuracyText = new();
            accuracyText.AddToClassList("accuracy_splash");
            accuracyText.AddToClassList(def.Class.ToString());
            accuracyText.text = def.Class.ToString();

            Sequence sequence = DOTween.Sequence();
            accuracy.Add(accuracyText);
            sequence.Append(DOTween.To(
                () => accuracyText.resolvedStyle.top,
                x => accuracyText.style.top = x,
                endValue: 0,
                duration: 0.3f
            ));
            sequence.Append(DOTween.To(
                () => accuracyText.resolvedStyle.opacity,
                x => accuracyText.style.opacity = x,
                endValue: 0,
                duration: 0.5f
            ));
            sequence.OnComplete(() =>
            {
                accuracyText.RemoveFromHierarchy();
            });
        }

        #endregion
        #region Streaks

        private void OnPlayerStreakStarted(StreakManager.PlayerStreakEvent evt)
        {
        }

        private void OnPlayerStreakEnded(StreakManager.PlayerStreakEvent evt)
        {
        }

        private void OnPlayerStreakUpdated(StreakManager.PlayerStreakEvent evt)
        {
            // Cache formatted string.
            if (!this.streakLabels.ContainsKey(evt.Value))
                this.streakLabels[evt.Value] = $"x{evt.Value}";

            // TODO: Update player streak.
        }

        private void OnTeamStreakStarted(StreakManager.TeamStreakEvent evt)
        {
        }

        private void OnTeamStreakEnded(StreakManager.TeamStreakEvent evt)
        {
        }

        private void OnTeamStreakUpdated(StreakManager.TeamStreakEvent evt)
        {
            // Cache formatted string.
            if (!this.streakLabels.ContainsKey(evt.Value))
                this.streakLabels[evt.Value] = $"x{evt.Value}";

            // Update team streak.
            this.teamStreak.text = this.streakLabels[evt.Value];
        }

        #endregion
    }
}
