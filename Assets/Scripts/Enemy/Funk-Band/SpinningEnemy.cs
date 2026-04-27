using UnityEngine;

//Royce Ortega
namespace Cadenza
{

    public class SpinningEnemy : Enemy
    {
        #region Variables

        private Vector3 direction;
        private float checkx, checkz;
        private float timer;

        #endregion

        // Do this in Start so that EnemyManager is initialized.
        void Start()
        {
            EnemyManager.AddEnemy(this);
            this.Initialize();
        }

        public override void Initialize()
        {
            this.runHealth = (int)(0.2 * this.maxHealth);
            this.hasRun = false;
            this.speed = 3f;
            this.isAttacking = false;
            this.isActionable = true;
            this.checkx = 0f;
            this.checkz = 0f;
            Vector2 temp = UnityEngine.Random.insideUnitCircle.normalized;
            this.direction = Vector3.Normalize(new Vector3(temp.x, 0f, temp.y));
        }

        protected override void FixedUpdate()
        {
            if (!this.IsGrounded())
            {
                this.rb.AddForce(Physics.gravity * 1f, ForceMode.Acceleration);
            }
        }

        private void Update()
        {
            if (this.checkx == this.gameObject.transform.position.x)
            {
                this.timer += Time.deltaTime;
            }
            else if (this.checkz == this.gameObject.transform.position.z)
            {
                this.timer += Time.deltaTime;
            }
            else
            {
                this.timer = 0f;
            }

            this.rb.linearVelocity = this.direction * this.speed;

            if (this.timer >= 0.1f)
            {
                this.timer = 0f;
                if (this.checkx == this.gameObject.transform.position.x)
                {
                    this.direction.x = -this.direction.x;
                }
                else if (this.checkz == this.gameObject.transform.position.z)
                {
                    this.direction.z = -this.direction.z;
                }
            }

            this.checkx = this.gameObject.transform.position.x;
            this.checkz = this.gameObject.transform.position.z;

            if (this.CheckIsDead())
            {
                this.DeadState();
            }

        }

        private void OnCollisionEnter(Collision collision)
        {
            if (this.currentHealth > 0)
            {
                if (collision.gameObject.CompareTag("SpinBorder") || collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Enemy"))
                {
                    /*
                    Vector3 surfaceNormal = collision.contacts[0].normal;
                    float angle = Vector3.Angle(this.direction, -surfaceNormal);
                    Debug.Log("Collision Angle: " + angle);
                    float tempF;
                    if(angle >= 45f)
                    {
                        tempF = 30f;
                    }
                    else
                    {
                        tempF = 0f;
                    }
                    */

                    bool temp = UnityEngine.Random.value > 0.5f;
                    this.direction = Vector3.Normalize(Quaternion.AngleAxis((temp ? 45f : -45f) + 180f, Vector3.up) * this.direction);
                }
                if (collision.gameObject.TryGetComponent(out Character character))
                {
                    if (!character.TakeDamage(3))
                        return;

                    // Add knockback.
                    Vector3 knockDirection = collision.transform.position - this.transform.position;
                    Vector3 force = knockDirection.normalized * 3;
                    force.y = 2f;
                    collision.gameObject.GetComponent<Rigidbody>().AddForce(force, ForceMode.Impulse);
                }
            }
        }

        public override void TakeDamage(int damage)
        {
            this.currentHealth -= damage;
            this.anim.SetTrigger("IsHit");

            // Hit stun.
            this.isActionable = false;
            this.Schedule(this.meleeDuration, () => this.isActionable = true);

            bool temp = UnityEngine.Random.value > 0.5f;
            this.direction = Vector3.Normalize(Quaternion.AngleAxis((temp ? 90f : -90f), Vector3.up) * this.direction);

            AudioSystem.PlayOneShotWithParameter(AudioSystem.PlayerOneShotsEvent, "ID", 3, immediate: true);
        }
    }
}
