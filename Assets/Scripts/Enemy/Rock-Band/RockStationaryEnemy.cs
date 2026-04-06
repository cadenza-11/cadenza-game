using UnityEngine;
using System;

//Royce Ortega
namespace Cadenza
{

    public class RockStationaryEnemy : Enemy
    {
        #region Variables
        [SerializeField] private GameObject meleePrefab;
        [SerializeField] protected Animator anim2;

        private float meleeTimer = 0.0f;
        private GameObject currentProjectile;
        private bool combatStarted = false;
        private int phase;
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
            this.meleeTimer = 0.0f;
            this.currentProjectile = null;
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
        }

        private void Update()
        {
            if (this.combatStarted)
            {
                this.phase = 4 - EnemyManager.EnemyCount;
                if (this.currentProjectile == null)
                {
                    this.RangedAttack();
                }
                this.FindNearestPlayerDist();
                if (this.nearestPlayerDist < 3.0f)
                {
                    this.meleeTimer += Time.deltaTime;
                }
                else
                {
                    this.meleeTimer = 0.0f;
                }
                if (this.meleeTimer >= 5.0f)
                {
                    this.meleeTimer = 0.0f;
                    this.MeleeAttack();
                }
                if (this.currentHealth <= 0)
                {
                    this.DeadState();
                }
            }
        }

        #region IEnemy Interface
        override protected void MeleeAttack()
        {
            GameObject meleeProjectile = Instantiate(this.meleePrefab, new Vector3(this.gameObject.transform.position.x, -0.1f, this.gameObject.transform.position.z), Quaternion.identity);

            // Play animation
            this.anim.SetTrigger("LightAttack");
            this.anim2.SetTrigger("LightAttack");
        }

        override protected void RangedAttack()
        {
            float[] wavePos = new float[10] {1.875f, 0.625f, -0.625f, -1.875f, 1.666f, 0.0f, -1.666f, 1.25f, 0.0f, -1.25f};
            this.anim.SetTrigger("LightAttack");
            this.anim2.SetTrigger("LightAttack");
            switch (this.phase)
            {
                case (1):
                    this.currentProjectile = Instantiate(this.projectile, new Vector3(0, -0.1f, wavePos[UnityEngine.Random.Range(0, 4)]), Quaternion.identity);
                    break;

                case (2):
                    this.currentProjectile = Instantiate(this.projectile, new Vector3(0, -0.1f, wavePos[UnityEngine.Random.Range(4, 7)]), Quaternion.identity);
                    this.currentProjectile.transform.localScale = new Vector3(1.0f, 1.0f, 1.333f);
                    break;

                case (3):
                    this.currentProjectile = Instantiate(this.projectile, new Vector3(0, -0.1f, wavePos[UnityEngine.Random.Range(7, 10)]), Quaternion.identity);
                    this.currentProjectile.transform.localScale = new Vector3(1.0f, 1.0f, 2f);
                    break;
            }
        }

        protected override void RangedAttack(Vector2 direction)
        {
            
        }

        override public void TakeDamage(int damage)
        {
            this.currentHealth -= damage;
            this.anim.SetTrigger("IsHit");
            this.anim2.SetTrigger("IsHit");

            // Hit stun.
            this.isActionable = false;
            this.Schedule(this.meleeDuration, () => this.isActionable = true);

            AudioSystem.PlayOneShotWithParameter(AudioSystem.PlayerOneShotsEvent, "ID", 3, immediate: true);
        }

        protected bool IsGrounded()
        {
            return Physics.Raycast(this.transform.position, Vector3.down, maxDistance: 0.1f);
        }

        /// <summary>
        /// Checks the enemy's current state and then goes into the proper State function for actions/state changes
        /// </summary>
        protected void CheckState()
        {
            if (!this.isActionable)
                return;

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

        /// <summary>
        /// Enemy's Idle State. Finds Enemy's distance to the nearest player and checks if it is within the bounds to enter the Ranged, Melee, or
        /// Chase states. Also checks health to see if Enemy should die.
        /// </summary>
        protected virtual void IdleState()
        {
            this.rb.linearVelocity = Vector3.zero;
            this.FindNearestPlayerDist();
            if (this.nearestPlayerDist < rangedDistance && this.nearestPlayerDist > chaseDistance)
            {
                this.meleeState = false;
                this.curState = EnemyState.Ranged;
            }
            else if (this.nearestPlayerDist < chaseDistance)
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
        protected void ChaseState()
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

        protected void MeleeState()
        {
            this.rb.linearVelocity = Vector3.zero;
            if (this.isAttacking)
                return;

            this.FindNearestPlayerDist();
            if (this.nearestPlayerDist > rangedDistance)
            {
                this.curState = EnemyState.Idle;
            }
            else if (this.nearestPlayerDist < rangedDistance && this.nearestPlayerDist > chaseDistance)
            {
                this.meleeState = false;
                this.curState = EnemyState.Ranged;
            }
            else if (this.nearestPlayerDist < chaseDistance && this.nearestPlayerDist > meleeDistance)
            {
                this.curState = EnemyState.Chase;
            }

            if (this.currentHealth <= this.runHealth & this.currentHealth > 0)
            {
                this.curState = EnemyState.Run;
            }
            else if (this.currentHealth <= 0)
            {
                this.curState = EnemyState.Dead;
            }

            if (UnityEngine.Random.Range(1, 100) <= 10)
            {
                this.curState = EnemyState.Special;
            }
            if (this.curState == EnemyState.Melee)
            {
                this.MeleeAttack();
            }
            else
            {
                this.isAttacking = false;
            }
        }

        protected void SpecialState()
        {
            this.rb.linearVelocity = Vector3.zero;
            //Do Special Move
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
                this.curState = EnemyState.Ranged;
            }
        }

        protected virtual void RunState()
        {
            /*if (!this.hasRun)
            {
                this.hasRun = true;
                this.TargetLocation = this.FindRunLocation();
            }
            This section could be useful for some enemies, but not for all. Will implement it in child enemy classes if need be.
            */

            Vector3 pos = this.transform.position;
            this.curAngle = (float)Math.Atan2(this.TargetLocation.y - pos.z, this.TargetLocation.x - pos.x);

            Vector3 moveDir = new Vector3(this.speed * (float)Math.Cos(this.curAngle), this.rb.linearVelocity.y, this.speed * (float)Math.Sin(this.curAngle));
            this.rb.linearVelocity = moveDir;

            if (Math.Abs(this.transform.position.x - this.TargetLocation.x) <= 0.2 &&
                Math.Abs(this.transform.position.z - this.TargetLocation.y) <= 0.2)
            {
                this.meleeState = false;
                this.curState = EnemyState.Ranged;
                this.rb.linearVelocity = Vector3.zero;
            }
            if (this.currentHealth <= 0)
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

        override protected void DeadState()
        {
            this.anim.SetBool("IsFainted", true);
            this.anim2.SetBool("IsFainted", true);
            this.rb.linearVelocity = Vector3.zero;
            EnemyManager.RemoveEnemy(this);
        }

        protected Vector2 FindNearestPlayerDist()
        {
            float nearest = float.MaxValue;
            foreach (var player in PlayerSystem.Players)
            {
                if (player.Character == null || player.Character.IsFainted)
                    continue;

                Vector3 playerPos = player.Character.transform.position;
                float curDistance = (float)Math.Sqrt(Math.Pow(this.transform.position.x - playerPos.x, 2) +
                                                Math.Pow(this.transform.position.z - playerPos.z, 2));
                if (curDistance < nearest)
                {
                    nearest = curDistance;
                    this.follow = player;
                }
            }
            //this.TargetLocation = this.follow.Character.transform.position;
            //The above line cannot be used in Multi-Directional. Will reimplement in future child-classes if needed
            this.nearestPlayerDist = nearest;
            return new Vector2(this.follow.Character.transform.position.x, this.follow.Character.transform.position.z);
        }

        protected Vector2 FindRunLocation()
        {
            this.FindNearestPlayerDist();
            Vector2 displacement = new Vector2(this.TargetLocation.x - this.transform.position.x, this.TargetLocation.y - this.transform.position.z);
            Vector2 runDirection = new Vector2(-1 * displacement.x / displacement.magnitude, -1 * displacement.y / displacement.magnitude);
            return new Vector3(this.transform.position.x + runDirection.x * 10, this.transform.position.y, this.transform.position.z + runDirection.y * 10);
        }

        protected void ChangeLinearVelocity()
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

        public int GetCurHealth()
        {
            return this.currentHealth;
        }

        public int GetMaxHealth()
        {
            return this.maxHealth;
        }
        #endregion
    }
}
