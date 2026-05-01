using UnityEngine;
using System;

//Royce Ortega
namespace Cadenza
{

    public class RockBassEnemy : Enemy
    {
        #region Variables
        [SerializeField] private GameObject chargeParticles;
        [SerializeField] private Colorway color;
        new protected const int chaseDistance = 8;
        new protected const int meleeDistance = 1;
        new protected const int rangedDistance = 30;
        private int phase;
        private int meleeTimer;
        private int beatCount;
        private bool specialOut;
        private bool currentlyCharging;
        private bool init = false;
        #endregion

        // Do this in Start so that EnemyManager is initialized.
        void Start()
        {
            EnemyManager.AddEnemy(this);
            this.sr.material.SetInt("_CharacterColor", 1);
            this.sr.material.SetColor("_PrimaryColor", this.color.PrimaryColor);
            this.sr.material.SetColor("_SecondaryColor", this.color.SecondaryColor);
            this.sr.material.SetColor("_TertiaryColor", this.color.TertiaryColor);
        }

        override public void Initialize()
        {
            this.runHealth = (int)(0.2 * this.maxHealth);
            this.hasRun = false;
            this.speed = 2.0f;
            this.isAttacking = false;
            this.isActionable = true;
            this.meleeTimer = 0;
            this.phase = 4 - EnemyManager.EnemyCount;
            this.beatCount = 0;
            this.specialOut = false;
            this.currentlyCharging = false;
            this.curAngle = 100;
            this.init = true;
            this.maxHealth *= PlayerSystem.PlayerCount;
            this.currentHealth *= PlayerSystem.PlayerCount;
            BeatSystem.BeatPlayed += this.onBeat;
        }

        private void onBeat()
        {
            this.beatCount++;
            this.meleeTimer++;
        }

        override protected void FixedUpdate()
        {
            if (!this.IsGrounded())
            {
                this.rb.AddForce(Physics.gravity * 1f, ForceMode.Acceleration);
            }
            if (this.init) {
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
            }

            //Checks if the Enemy's state needs to change
            this.phase = 4 - EnemyManager.EnemyCount;
            this.CheckState();
        }

        #region IEnemy Interface

        override protected void RangedAttack()
        {
            //no ranged attack
        }

        protected override void RangedAttack(Vector2 direction)
        {
            //no ranged attack
        }

        /// <summary>
        /// Enemy's Idle State. Finds Enemy's distance to the nearest player and checks if it is within the bounds to enter the Ranged, Melee, or
        /// Chase states. Also checks health to see if Enemy should die.
        /// </summary>
        override protected void IdleState()
        {
            this.rb.linearVelocity = Vector3.zero;
            this.FindNearestPlayerDist();
            if (this.nearestPlayerDist < rangedDistance)
            {
                this.curState = EnemyState.Chase;
            }

            if (this.currentHealth <= 0)
            {
                this.curState = EnemyState.Dead;
            }
        }

        /// <summary>
        /// Enemy's Chase State. Moves the enemy towards the selected closest Player.
        /// </summary>
        override protected void ChaseState()
        {
            this.anim.SetBool("IsMove", true);
            this.TargetLocation = this.FindNearestPlayerDist();
            this.curAngle = (float)Math.Atan2(this.TargetLocation.y - this.transform.position.z, this.TargetLocation.x - this.transform.position.x);

            Vector3 moveDir = new Vector3(this.speed * (float)Math.Cos(this.curAngle), this.rb.linearVelocity.y, this.speed * (float)Math.Sin(this.curAngle));
            this.rb.linearVelocity = moveDir;

            this.FindNearestPlayerDist();
            //Move Towards target location here
            if (this.nearestPlayerDist > rangedDistance)
            {
                this.anim.SetBool("IsMove", false);
                this.curState = EnemyState.Idle;
                this.rb.linearVelocity = Vector3.zero;
            }
            else if (this.nearestPlayerDist <= meleeDistance)
            {
                this.anim.SetBool("IsMove", false);
                this.meleeState = true;
                this.curState = EnemyState.Melee;
                this.rb.linearVelocity = Vector3.zero;
            }

            if (this.currentHealth <= 0)
            {
                this.anim.SetBool("IsMove", false);
                this.curState = EnemyState.Dead;
                this.rb.linearVelocity = Vector3.zero;
            }
        }

        override protected void MeleeState()
        {
            this.rb.linearVelocity = Vector3.zero;
            if (this.isAttacking)
                return;

            this.FindNearestPlayerDist();
            if (this.nearestPlayerDist > rangedDistance)
            {
                this.curState = EnemyState.Idle;
            }
            else if (this.nearestPlayerDist < rangedDistance && this.nearestPlayerDist > meleeDistance)
            {
                this.curState = EnemyState.Chase;
            }

            if (this.currentHealth <= 0)
            {
                this.curState = EnemyState.Dead;
            }

            if (UnityEngine.Random.Range(1, 100) <= 20 && this.beatCount >= 4 && this.meleeTimer == 1)
            {
                this.beatCount = 0;
                this.curState = EnemyState.Special;
            }
            if (this.curState == EnemyState.Melee && this.meleeTimer == 1)
            {
                this.MeleeAttack();
            }
            else
            {
                this.isAttacking = false;
            }
            this.meleeTimer = 0;
        }

        override protected void SpecialState()
        {
            this.rb.linearVelocity = Vector3.zero;
            if(this.specialOut == false)
            {
                GameObject projectileInstance = Instantiate(this.projectile, new Vector3(this.gameObject.transform.position.x, -0.125f, this.gameObject.transform.position.z), Quaternion.identity);
                switch (this.phase)
                {
                    case (1):
                        projectileInstance.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                        break;

                    case (2):
                        projectileInstance.transform.localScale = new Vector3(1.25f, 1.0f, 1.25f);
                        break;

                    case (3):
                        projectileInstance.transform.localScale = new Vector3(1.5f, 1.0f, 1.5f);
                        break;
                }
                this.specialOut = true;
                this.rb.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
            if(this.beatCount == 1 && this.currentlyCharging == false)
            {
                this.currentlyCharging = true;
                this.anim.SetTrigger("LightAttack");
                this.chargeParticles.SetActive(true);
            }
            else if (this.beatCount == 3 && this.currentlyCharging == true)
            {
                this.chargeParticles.SetActive(false);
                this.currentlyCharging = false;
                this.anim.SetTrigger("LightAttack");
            }
            else if (this.beatCount >= 4)
            {
                this.beatCount = 0;
                this.specialOut = false;
                this.rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                if (this.meleeState)
                {
                    this.meleeState = true;
                    this.curState = EnemyState.Melee;
                }
                else if (!this.hasRun && this.currentHealth < this.runHealth && this.currentHealth >= 0)
                {
                    this.curState = EnemyState.Run;
                }
                else if (this.currentHealth <= 0)
                {
                    this.curState = EnemyState.Dead;
                }
                else
                {
                    this.meleeState = false;
                    this.curState = EnemyState.Chase;
                }
            }
        }

        override protected void RangedState()
        {
            //no ranged attack
        }
        #endregion
    }
}
