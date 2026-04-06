using UnityEngine;


namespace Cadenza
{
    public class RockStationaryWaveHitbox : MonoBehaviour
    {
        public int damage = 0;
        public float knockbackScale;
        private GameObject go = null;
        private float t;


        public void SetActive(bool enabled)
        {
            if (this.go == null)
                this.go = this.gameObject;

            this.go.SetActive(enabled);

            this.t = 0.0f;
        }

        public void Update()
        {
            if (this.gameObject.activeSelf)
            {
                this.transform.position = new Vector3(Mathf.Lerp(25.0f, -25.0f, this.t), 0.25f, this.gameObject.transform.parent.position.z);

                this.t += 0.25f * Time.deltaTime;

                if (this.t >= 1)
                {
                    Destroy(this.transform.parent.gameObject);
                }
            }
        }


        private void OnTriggerEnter(Collider collider)
        {
            // Hit player.
            if (collider.TryGetComponent(out Character character))
            {
                float phaseMult = 2.5f - (0.5f * EnemyManager.EnemyCount);
                int newDamage = (int)(this.damage * phaseMult);
                if (!character.TakeDamage(newDamage))
                    return;

                // Add knockback.
                Vector3 direction = collider.transform.position - this.transform.position;
                Vector3 force = direction.normalized * this.knockbackScale;
                force.y = 2f;
                collider.attachedRigidbody.AddForce(force, ForceMode.Impulse);
            }
        }
    }
}
