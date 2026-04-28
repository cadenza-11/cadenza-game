using UnityEngine;

namespace Cadenza
{
    public class EnemyAttackArea : MonoBehaviour
    {
        public int damage = 0;
        public float knockbackScale;
        public AttkEffect comboMove = AttkEffect.None;
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
                if (!character.TakeDamage(this.damage, out float knockbackMultiplier))
                    return;

                // Add knockback.
                Vector3 direction = collider.transform.position - this.transform.position;
                Vector3 force = direction.normalized * this.knockbackScale;
                force.y = 2f;
                force *= knockbackMultiplier;
                collider.attachedRigidbody.AddForce(force, ForceMode.Impulse);
            }

            this.comboMove = AttkEffect.None;
        }
    }
}
