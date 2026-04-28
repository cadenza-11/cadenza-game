using UnityEngine;

namespace Cadenza.Combo
{
    public class ComboManager : MonoBehaviour
    {
        [SerializeField] public ComboDatabase ComboDB;

        private ComboNode m_ComboRoot;
        private ComboNode m_CurrentComboNode;
        private bool m_InProgress;
        private int m_ExpectedBeat;
        private int m_LastMoveBeat = int.MinValue;
        private bool m_IsChargeHeld;

        public void Start()
        {
            if (this.ComboDB == null)
            {
                Debug.LogWarning($"{nameof(ComboManager)} on {this.name} is missing a combo database.");
                this.m_ComboRoot = new ComboNode();
                this.ResetComboProgress();
                return;
            }

            this.m_ComboRoot = this.ComboDB.BuildTree();
            this.ResetComboProgress();
        }

        public bool ProcessCombo(AttkTypes move, ScoreDef score, out ComboReward reward)
        {
            reward = DefaultReward();

            if (move == AttkTypes.Rest)
                return false;

            this.ExpireProgress(score.Timestamp);

            if (score.Class == ScoreClass.Bad)
            {
                this.ResetComboProgress();
                return false;
            }

            return this.ProcessMove(move, score.Beat, out reward);
        }

        public bool ProcessHeldCharge(int beat)
        {
            if (beat == this.m_LastMoveBeat)
                return false;

            return this.ProcessMove(AttkTypes.Charge, beat, out _);
        }

        public void SetChargeHeld(bool isHeld)
        {
            this.m_IsChargeHeld = isHeld;
        }

        public void Update()
        {
            if (!this.m_InProgress)
                return;

            this.ExpireProgress(BeatSystem.CurrentTrackTime);
        }

        private bool ProcessMove(AttkTypes move, int beat, out ComboReward reward)
        {
            reward = DefaultReward();

            if (this.m_InProgress)
            {
                if (beat < this.m_ExpectedBeat)
                    return false;

                if (this.IsWaitingForRest())
                {
                    this.ResetComboProgress();
                    return this.TryStartCombo(move, beat, out reward);
                }

                if (beat == this.m_ExpectedBeat)
                {
                    if (this.TryAdvance(move, beat, out reward))
                        return true;

                    this.ResetComboProgress();
                    return this.TryStartCombo(move, beat, out reward);
                }

                this.ResetComboProgress();
            }

            return this.TryStartCombo(move, beat, out reward);
        }

        private bool TryStartCombo(AttkTypes move, int beat, out ComboReward reward)
        {
            reward = DefaultReward();

            if (move == AttkTypes.Rest)
                return false;

            if (!this.m_ComboRoot.Children.ContainsKey(move))
                return false;

            return this.TryAdvance(move, beat, out reward);
        }

        private bool TryAdvance(AttkTypes move, int beat, out ComboReward reward)
        {
            reward = DefaultReward();

            if (!this.m_CurrentComboNode.Children.TryGetValue(move, out var next))
                return false;

            this.m_InProgress = true;
            this.m_CurrentComboNode = next;
            this.m_LastMoveBeat = beat;
            this.m_ExpectedBeat = beat + 1;

            if (this.m_CurrentComboNode.IsEnd)
            {
                reward = this.m_CurrentComboNode.Reward;
                AudioSystem.PlayOneShot(Sound.Gameplay.ComboSucceeded, immediate: true);
                this.ResetComboProgress();
            }

            return true;
        }

        private void ExpireProgress(double currentTrackTime)
        {
            if (!this.m_InProgress || BeatSystem.SecondsPerBeat <= 0)
                return;

            while (this.m_InProgress && currentTrackTime >= this.GetBeatTime(this.m_ExpectedBeat) + this.GraceSeconds)
            {
                if (this.m_IsChargeHeld
                    && (this.m_CurrentComboNode.Children.ContainsKey(AttkTypes.Charge) || this.IsWaitingForRest()))
                {
                    return;
                }

                if (this.IsWaitingForRest())
                {
                    this.TryAdvance(AttkTypes.Rest, this.m_ExpectedBeat, out _);
                    continue;
                }

                this.ResetComboProgress();
            }
        }

        private bool IsWaitingForRest()
        {
            return this.m_CurrentComboNode.Children.ContainsKey(AttkTypes.Rest);
        }

        private double GetBeatTime(int beat)
        {
            return beat * BeatSystem.SecondsPerBeat;
        }

        private double GraceSeconds => ScoreSystem.IndividualThresholds.okScoreMs / 1000d;

        private void ResetComboProgress()
        {
            this.m_CurrentComboNode = this.m_ComboRoot;
            this.m_InProgress = false;
            this.m_ExpectedBeat = 0;
            this.m_LastMoveBeat = int.MinValue;
        }

        private static ComboReward DefaultReward()
        {
            return new ComboReward
            {
                AttackEffect = AttkEffect.None,
                Knockback = 3,
                Multiplier = 1
            };
        }
    }
}
