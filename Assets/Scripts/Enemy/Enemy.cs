using UnityEngine;
using System;

//Royce Ortega
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
        [SerializeField] public float speed;
        [SerializeField] private float meleeDuration;
        [SerializeField] private float rangedDuration;
        [SerializeField] private int maxHealth;
        [SerializeField] public int currentHealth;
        [SerializeField] private AttackArea attackArea;
        [SerializeField] private Rigidbody rb;
        [SerializeField] private SpriteRenderer sr;
        [SerializeField] private Animator anim;
        [SerializeField] private GameObject projectile;

        private float rangedAttackInterval = 0f;
        [SerializeField] private EnemyState curState = EnemyState.Idle;
        [SerializeField] private EnemyManager enemyMgr;
        private const int chaseDistance = 20;
        private const int meleeDistance = 1;
        private const int rangedDistance = 50;
        private bool meleeState;
        private bool hasRun;
        private bool isAttacking;
        private int attackMod;
        private int runHealth;
        private float nearestPlayerDist;
        private float curAngle;
        private Player follow;
        private Vector2 TargetLocation;
        private bool isActionable;

        //May want a character manager to see character locations
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            this.runHealth = (int)(0.2 * this.maxHealth);
            this.hasRun = false;
            this.speed = 1.5f;
            this.isAttacking = false;
            this.enemyMgr.AddEnemy(this.gameObject);
            this.isActionable = true;
        }

        // Update is called once per frame
        void FixedUpdate()
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
            this.CheckState();
        }

        #region IEnemy Interface
        private void MeleeAttack()
        {
            this.isAttacking = true;
            this.attackArea.SetActive(true);
            this.attackMod = 1;
            this.attackArea.damage = 1;

            this.Schedule(this.meleeDuration * this.attackMod, () =>
            {
                this.isAttacking = false;
                this.attackArea.SetActive(false);
            });

            // Play animation
            this.anim.SetTrigger("LightAttack");
        }

        private void RangedAttack()
        {
            GameObject projectileInstance = Instantiate(this.projectile, this.gameObject.transform.position, Quaternion.identity);
            projectileInstance.GetComponent<Projectile>().direction =
                (this.curAngle * (180 / Math.PI) > -90 && this.curAngle * (180 / Math.PI) < 90) ? true : false;
            projectileInstance.GetComponent<Projectile>().speedSet = false;
            this.anim.SetTrigger("LightAttack");
        }

        public void DoDamage(int damage)
        {
            this.currentHealth -= damage;
            this.anim.SetTrigger("IsHit");

            // Hit stun.
            this.isActionable = false;
            this.Schedule(this.meleeDuration, () => this.isActionable = true);

            AudioSystem.PlayOneShotWithParameter(AudioSystem.PlayerOneShotsEvent, "ID", 3, immediate: true);
        }

        private bool IsGrounded()
        {
            return Physics.Raycast(this.transform.position, Vector3.down, maxDistance: 0.1f);
        }

        /// <summary>
        /// Checks the enemy's current state and then goes into the proper State function for actions/state changes
        /// </summary>
        private void CheckState()
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
        private void IdleState()
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
        private void ChaseState()
        {
            this.FindNearestPlayerDist();
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

        private void MeleeState()
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

        private void SpecialState()
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

        private void RunState()
        {
            if (!this.hasRun)
            {
                this.hasRun = true;
                this.TargetLocation = this.FindRunLocation();
            }

            Vector3 pos = this.transform.position;
            this.curAngle = (float)Math.Atan2(this.TargetLocation.y - pos.z, this.TargetLocation.x - pos.x);

            Vector3 moveDir = new Vector3(this.speed * (float)Math.Cos(this.curAngle), this.rb.linearVelocity.y, this.speed * (float)Math.Sin(this.curAngle));
            this.rb.linearVelocity = moveDir;

            if (Math.Abs(this.transform.position.x - this.TargetLocation.x) <= 0.1 &&
                Math.Abs(this.transform.position.z - this.TargetLocation.y) <= 0.1)
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

        private void RangedState()
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
                this.rangedAttackInterval = 1.5f;
            }
            else
            {
                this.rangedAttackInterval -= Time.deltaTime;
            }

        }

        private void DeadState()
        {
            this.anim.SetBool("IsFainted", true);
            this.rb.linearVelocity = Vector3.zero;
            this.enemyMgr.RemoveEnemy(this.gameObject);
        }

        private void FindNearestPlayerDist()
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
            this.TargetLocation = this.follow.Character.transform.position;
            this.nearestPlayerDist = nearest;
        }

        private Vector2 FindRunLocation()
        {
            this.FindNearestPlayerDist();
            Vector2 displacement = new Vector2(this.TargetLocation.x - this.transform.position.x, this.TargetLocation.y - this.transform.position.z);
            Vector2 runDirection = new Vector2(-1 * displacement.x / displacement.magnitude, -1 * displacement.y / displacement.magnitude);
            return new Vector3(this.transform.position.x + runDirection.x * 10, this.transform.position.y, this.transform.position.z + runDirection.y * 10);
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
