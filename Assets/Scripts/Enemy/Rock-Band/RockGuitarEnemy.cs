using UnityEngine;
using System;

//Royce Ortega
namespace Cadenza
{

    public class RockGuitarEnemy : Enemy
    {
        #region Variables
        new protected const int chaseDistance = 8;
        new protected const int meleeDistance = 1;
        new protected const int rangedDistance = 30;
        private int phase;
        private float rangedTimer = 0.0f;
        private bool combatStarted = false;
        #endregion

        // Do this in Start so that EnemyManager is initialized.
        void Start()
        {
            EnemyManager.AddEnemy(this);
        }

        override public void Initialize()
        {
            this.runHealth = (int)(0.2 * this.maxHealth);
            this.hasRun = false;
            this.speed = 1.5f;
            this.isAttacking = false;
            this.isActionable = true;
            this.rangedTimer = 0.0f;
            this.phase = 4 - EnemyManager.EnemyCount;
            this.combatStarted = true;
        }

        override protected void FixedUpdate()
        {
            if (!this.IsGrounded())
            {
                this.rb.AddForce(Physics.gravity * 1f, ForceMode.Acceleration);
            }
            if (this.curAngle * (180 / Math.PI) > -90 && this.curAngle * (180 / Math.PI) < 90)
            {
                // No rotation needed.
                this.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            else
            {
                // Turn character to the left.
                this.transform.rotation = Quaternion.Euler(0, 180, 0);
            }

            //Checks if the Enemy's state needs to change
            this.phase = 4 - EnemyManager.EnemyCount;
            this.CheckState();
        }

        private void Update()
        {
            if (this.combatStarted)
            {
                if (this.curState != EnemyState.Ranged)
                {
                    this.rangedTimer += Time.deltaTime;
                }
                else
                {
                    this.rangedTimer = 0.0f;
                }
                if (this.rangedTimer >= 10.0f)
                {
                    this.rangedTimer = 0.0f;
                    this.RangedAttack();
                }
            }
        }

        #region IEnemy Interface

        override protected void RangedAttack()
        {
            this.anim.SetTrigger("LightAttack");
            for (int i = 0; i < 3 * this.phase; i++)
            {
                GameObject projectileInstance = Instantiate(this.projectile, new Vector3(UnityEngine.Random.Range(-15.0f, 15.0f), -0.1f, UnityEngine.Random.Range(-2.5f, 2.5f)), Quaternion.identity);
            }
        }

        protected override void RangedAttack(Vector2 direction)
        {
            this.anim.SetTrigger("LightAttack");
            for (int i = 0; i < 3 * this.phase; i++)
            {
                GameObject projectileInstance = Instantiate(this.projectile, new Vector3(UnityEngine.Random.Range(-15.0f, 15.0f), -0.1f, UnityEngine.Random.Range(-2.5f, 2.5f)), Quaternion.identity);
            }
        }

        /// <summary>
        /// Enemy's Chase State. Moves the enemy towards the selected closest Player.
        /// </summary>
        override protected void ChaseState()
        {
            this.TargetLocation = this.FindNearestPlayerDist();
            this.curAngle = (float)Math.Atan2(this.TargetLocation.y - this.transform.position.z, this.TargetLocation.x - this.transform.position.x);

            Vector3 moveDir = new Vector3(this.speed * (float)Math.Cos(this.curAngle), this.rb.linearVelocity.y, this.speed * (float)Math.Sin(this.curAngle));
            this.rb.linearVelocity = moveDir;

            this.FindNearestPlayerDist();
            //Move Towards target location here
            if (this.nearestPlayerDist > rangedDistance)
            {
                this.curState = EnemyState.Idle;
                this.rb.linearVelocity = Vector3.zero;
            }
            else if (this.nearestPlayerDist < rangedDistance && this.nearestPlayerDist > chaseDistance)
            {
                this.meleeState = false;
                this.curState = EnemyState.Ranged;
                this.rb.linearVelocity = Vector3.zero;
            }
            else if (this.nearestPlayerDist <= meleeDistance)
            {
                this.meleeState = true;
                this.curState = EnemyState.Melee;
                this.rb.linearVelocity = Vector3.zero;
            }

            if (this.currentHealth < this.runHealth && this.currentHealth > 0)
            {
                this.curState = EnemyState.Run;
                this.rb.linearVelocity = Vector3.zero;
            }
            else if (this.currentHealth <= 0)
            {
                this.curState = EnemyState.Dead;
                this.rb.linearVelocity = Vector3.zero;
            }
        }

        override protected void RangedState()
        {
            this.rb.linearVelocity = Vector3.zero;
            this.FindNearestPlayerDist();
            this.curAngle = (float)Math.Atan2(this.TargetLocation.y - this.transform.position.z, this.TargetLocation.x - this.transform.position.x);
            if (this.nearestPlayerDist > rangedDistance)
            {
                this.curState = EnemyState.Idle;
            }
            else if (!this.hasRun && this.nearestPlayerDist < chaseDistance && this.nearestPlayerDist > meleeDistance)
            {
                this.curState = EnemyState.Chase;
            }
            else if (!this.hasRun && this.nearestPlayerDist <= meleeDistance)
            {
                this.meleeState = true;
                this.curState = EnemyState.Melee;
            }

            if (!this.hasRun && this.currentHealth <= this.runHealth && this.currentHealth > 0)
            {
                this.curState = EnemyState.Run;
            }
            else if (this.currentHealth <= 0)
            {
                this.curState = EnemyState.Dead;
            }
            if (this.rangedAttackInterval <= 0)
            {
                this.RangedAttack();
                this.rangedAttackInterval = 3f;
            }
            else
            {
                this.rangedAttackInterval -= Time.deltaTime;
            }

        }
        #endregion
    }
}
