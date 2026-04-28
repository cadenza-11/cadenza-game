using UnityEngine;


namespace Cadenza
{
    public class RockBassShockwaveHitbox : MonoBehaviour
    {
        public int damage = 0;
        public float knockbackScale;
        private GameObject go = null;

        public void SetActive(bool enabled)
        {
            if (this.go == null)
                this.go = this.gameObject;

            this.go.SetActive(enabled);
        }


        private void OnTriggerEnter(Collider collider)
        {
            // Hit player.
            if (collider.TryGetComponent(out Character character))
            {
                float phaseMult = 2.5f - (0.5f * EnemyManager.EnemyCount);
                int newDamage = (int)(this.damage * phaseMult);
                if (!character.TakeDamage(newDamage, out float knockbackMultiplier))
                    return;

                // Add knockback.
                Vector3 direction = collider.transform.position - this.transform.position;
                Vector3 force = direction.normalized * this.knockbackScale;
                force.y = 2f;
                force *= knockbackMultiplier;
                collider.attachedRigidbody.AddForce(force, ForceMode.Impulse);
            }
        }
    }
}
