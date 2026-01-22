using UnityEngine;

namespace Cadenza.Combo
{
    public class ComboManager : MonoBehaviour
    {
        [SerializeField] public ComboDatabase ComboDB;

        private ComboNode m_ComboRoot;

        private ComboNode m_CurrentComboNode;

        private float m_Timer = 0.0f;

        private bool m_InProgress = false;

        public void Start()
        {
            this.m_ComboRoot = this.ComboDB.BuildTree();
        }

        public bool ProcessCombo(AttkTypes move, out ComboReward reward)
        {
            reward = new ComboReward
            {
                AttackEffect = AttkEffect.None,
                Multiplier = 1
            };

            if (this.m_CurrentComboNode.Children.TryGetValue(move, out var next))
            {
                this.m_InProgress = true;
                this.m_CurrentComboNode = next;

                if (this.m_CurrentComboNode.IsEnd)
                {
                    reward = this.m_CurrentComboNode.Reward;
                    this.ResetComboProgress();
                }

                return true;
            }

            this.ResetComboProgress();
            return false;
        }

        private void ResetComboProgress()
        {
            this.m_CurrentComboNode = this.m_ComboRoot;
            this.m_InProgress = false;
            this.m_Timer = this.ComboDB.ComboTimeout;
        }

        public void Update()
        {
            if (!this.m_InProgress) return;

            this.m_Timer -= Time.deltaTime;
            if (this.m_Timer <= 0)
            {
                this.ResetComboProgress();
            }
        }
    }
}
