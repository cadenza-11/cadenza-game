using UnityEngine;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using System;

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
        Dead
    }

    public class Enemy : MonoBehaviour
    {
        [SerializeField] private Transform Transform;
        [SerializeField] public int speed;
        [SerializeField] private float meleeDuration;
        [SerializeField] private float rangedDuration;
        [SerializeField] private int maxHealth;
        [SerializeField] public int currentHealth;
        [SerializeField] private AttackArea attackArea;
        [SerializeField] private AttackArea chargeArea;
        [SerializeField] private Rigidbody rb;
        [SerializeField] private SpriteRenderer sr;
        [SerializeField] private Animator anim;
        private float attackTimer = 0f;
        private EnemyState curState = EnemyState.Idle;
        private const int chaseDistance = 20;
        private const int meleeDistance = 2;
        private const int rangedDistance = 50;
        private bool meleeState;
        private bool hasRun;
        private  int runHealth;
        private float nearestPlayerDist;
        private Player follow;
        private Vector2 TargetLocation;

        //May want a character manager to see character locations
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            this.Transform = this.GetComponent<Transform>();
            this.runHealth = (int)(0.2 * this.maxHealth);
            this.hasRun = false;
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

        public void TakeDamage()
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
                case EnemyState.Dead:
                    this.DeadState();
                    break;
            }
        }

        private void IdleState()
        {
            this.FindNearestPlayerDist();
            if(this.nearestPlayerDist < rangedDistance && this.nearestPlayerDist > chaseDistance)
            {
                this.curState = EnemyState.Ranged;
            }
            else if(this.nearestPlayerDist < chaseDistance)
            {
                this.curState = EnemyState.Chase;
            }

            if(this.currentHealth <= 0)
            {
                this.curState = EnemyState.Dead;
            }
        }

        private void ChaseState()
        {
            this.FindNearestPlayerDist();
            //Move Towards target location here
            if(this.nearestPlayerDist > rangedDistance)
            {
                this.curState = EnemyState.Idle;
            }
            else if(this.nearestPlayerDist < rangedDistance && this.nearestPlayerDist > chaseDistance)
            {
                this.curState = EnemyState.Ranged;
            }
            else if(this.nearestPlayerDist <= meleeDistance)
            {
                this.curState = EnemyState.Melee;
            }

            if(this.currentHealth < this.runHealth && this.currentHealth > 0)
            {
                this.curState = EnemyState.Run;
            }
            else if(this.currentHealth <= 0)
            {
                this.curState = EnemyState.Dead;
            }
        }

        private void MeleeState()
        {
            //Do Melee attack here
            this.meleeState = true;
            this.FindNearestPlayerDist();
            if(this.nearestPlayerDist > rangedDistance)
            {
                this.curState = EnemyState.Idle;
            }
            else if(this.nearestPlayerDist < rangedDistance && this.nearestPlayerDist > chaseDistance)
            {
                this.curState = EnemyState.Ranged;
            }
            else if(this.nearestPlayerDist < chaseDistance && this.nearestPlayerDist > meleeDistance)
            {
                this.curState = EnemyState.Chase;
            }

            if(this.currentHealth <= this.runHealth & this.currentHealth > 0)
            {
                this.curState = EnemyState.Run;
            }
            else if(this.currentHealth <= 0)
            {
                this.curState = EnemyState.Dead;
            }

            System.Random rand = new System.Random();
            if(rand.Next(1, 100) <= 10)
            {
                this.curState = EnemyState.Special;
            }

        }

        private void SpecialState()
        {
            //Do Special Move
            if(this.meleeState)
            {
                this.curState = EnemyState.Melee;
            }
            else if(!this.hasRun && this.currentHealth < this.runHealth && this.currentHealth >= 0)
            {
                this.curState = EnemyState.Run;
            }
            else if(this.currentHealth <= 0)
            {
                this.curState = EnemyState.Dead;
            }
            else
            {
                this.curState = EnemyState.Ranged;
            }
        }

        private void RunState()
        {
            if(!this.hasRun)
            {
                this.hasRun = true;
                this.TargetLocation = this.FindRunLocation();
            }

            //Move towards this.TargetLocation

            if(this.Transform.position.x == this.TargetLocation.x && this.Transform.position.y == this.TargetLocation.y)
            {
                this.curState = EnemyState.Ranged;
            }
            if(this.currentHealth <= 0)
            {
                this.curState = EnemyState.Dead;
            }
        }

        private void RangedState()
        {
            if(this.nearestPlayerDist > rangedDistance)
            {
                this.curState = EnemyState.Idle;
            }
            else if(!this.hasRun && this.nearestPlayerDist < chaseDistance && this.nearestPlayerDist > meleeDistance)
            {
                this.curState = EnemyState.Chase;
            }
            else if(!this.hasRun && this.nearestPlayerDist <= meleeDistance)
            {
                this.curState = EnemyState.Melee;
            }

            if(!this.hasRun && this.currentHealth <= this.runHealth && this.currentHealth > 0)
            {
                this.curState = EnemyState.Run;
            }
            else if(this.currentHealth <= 0)
            {
                this.curState = EnemyState.Dead;
            }
        }

        private void DeadState()
        {
            EnemyManager.singleton.RemoveEnemy(this.gameObject);
        }

        private void FindNearestPlayerDist()
        {
            float nearest = 99999999999999;
            foreach(KeyValuePair<int, Player> player in PlayerSystem.PlayersByID)
            {
                Vector2 playerPos = player.Value.Character.GetLocation();
                float curDistance = (float)Math.Sqrt(Math.Pow(this.Transform.position.x - playerPos.x, 2) + 
                                                Math.Pow(this.Transform.position.y - playerPos.y, 2));
                if(curDistance < nearest)
                {
                    nearest = curDistance;
                    this.follow = player.Value;
                }
            }
            this.nearestPlayerDist = nearest;
        }

        private Vector2 FindRunLocation()
        {
            this.FindNearestPlayerDist();
            Vector2 nearestPlayer = this.follow.Character.GetLocation();
            Vector2 displacement = new Vector2(this.Transform.position.x - nearestPlayer.x, this.Transform.position.y - nearestPlayer.y);
            Vector2 runDirection = new Vector2(-1 * displacement.x / displacement.magnitude, -1 * displacement.y / displacement.magnitude);
            return new Vector2(this.Transform.position.x + runDirection.x * 30, this.Transform.position.y + runDirection.y * 30);
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
