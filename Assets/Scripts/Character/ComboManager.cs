using Cadenza;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
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
            m_ComboRoot = ComboDB.BuildTree();
        }

        public bool ProcessCombo(AttkTypes move, out ComboReward reward)
        {
            reward = new ComboReward
            {
                AttackEffect = AttkEffect.None,
                Multiplier = 1
            };

            if (m_CurrentComboNode.Children.TryGetValue(move, out var next))
            {
                m_InProgress = true;
                m_CurrentComboNode = next;

                if (m_CurrentComboNode.IsEnd)
                {
                    reward = m_CurrentComboNode.Reward;
                    ResetComboProgress();
                }

                return true;
            }

            ResetComboProgress();
            return false;
        }

        private void ResetComboProgress()
        {
            m_CurrentComboNode = m_ComboRoot;
            m_InProgress = false;
            m_Timer = ComboDB.ComboTimeout;
        }

        public void Update()
        {
            if (!m_InProgress) return;

            m_Timer -= Time.deltaTime;
            if (m_Timer <= 0)
            {
                ResetComboProgress();
            }
        }
    }
}
