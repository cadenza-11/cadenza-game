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

        override protected void DeadState()
        {
            this.anim.SetBool("IsFainted", true);
            this.anim2.SetBool("IsFainted", true);
            this.rb.linearVelocity = Vector3.zero;
            EnemyManager.RemoveEnemy(this);
        }
        #endregion
    }
}
