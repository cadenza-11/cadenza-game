using UnityEngine;
using System.Collections.Generic;

namespace Cadenza
{
    public enum EnemyState
    {
        Idle,
        Chase,
        Melee,
        Special,
        Run,
        Ranged,
    }

    public class Enemy : MonoBehaviour
    {
        [SerializeField] private Transform Transform;
        [SerializeField] private int speed;
        [SerializeField] private float meleeDuration;
        [SerializeField] private float rangedDuration;
        [SerializeField] private int currentHealth;
        [SerializeField] private AttackArea attackArea;
        [SerializeField] private AttackArea chargeArea;
        [SerializeField] private Rigidbody rb;
        [SerializeField] private SpriteRenderer sr;
        [SerializeField] private Animator anim;
        private float attackTimer = 0f;
        private EnemyState curState = EnemyState.Idle;

        //May want a character manager to see character locations
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            IReadOnlyDictionary<int, Player> test = PlayerSystem.PlayersByID;
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            this.CheckState();
        }

        #region IEnemy Interface
        private void MeleeAttack()
        {

        }
        /// <summary>
        ///  
        /// </summary>
        private void RangedAttack()
        {

        }

        private void SpecialAttack()
        {

        }

        private void DoDamage()
        {

        }

        /// <summary>
        /// Checks the enemy's current state and then goes into the proper State function for actions/state changes
        /// </summary>
        private void CheckState()
        {
            switch (this.curState)
            {
                case EnemyState.Idle:
                    this.IdleState();
                    break;
                case EnemyState.Chase:
                    this.ChaseState();
                    break;
                case EnemyState.Melee:
                    this.MeleeState();
                    break;
                case EnemyState.Special:
                    this.SpecialState();
                    break;
                case EnemyState.Run:
                    this.RunState();
                    break;
                case EnemyState.Ranged:
                    this.RangedState();
                    break;
            }
        }

        private void IdleState()
        {

        }

        private void ChaseState()
        {

        }

        private void MeleeState()
        {

        }

        private void SpecialState()
        {

        }

        private void RunState()
        {

        }

        private void RangedState()
        {

        }

        public bool CheckIsDead()
        {
            if (this.currentHealth <= 0)
            {
                return true;
            }
            return false;
        }
        #endregion
    }
}
