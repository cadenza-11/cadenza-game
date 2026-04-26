using UnityEngine;

namespace Cadenza
{
    public class ZoomerAttack : MonoBehaviour
    {
        public int damage = 0;
        public float knockbackScale;
        public AttkEffect comboMove = AttkEffect.None;
        private GameObject go = null;
        private int numCollisions = 0;

        public void SetActive(bool enabled)
        {
            if (this.go == null)
                this.go = this.gameObject;

            this.go.SetActive(enabled);
        }


        private void OnTriggerEnter(Collider collider)
        {
            Debug.Log("Goes into isTrigger");
            // Hit player.
            if (collider.TryGetComponent(out Character character))
            {
                if (!character.TakeDamage(this.damage))
                    return;

                // Add knockback.
                Vector3 direction = collider.transform.position - this.transform.position;
                Vector3 force = direction.normalized * this.knockbackScale;
                force.y = 2f;
                collider.attachedRigidbody.AddForce(force, ForceMode.Impulse);
            }
            else if(collider.CompareTag("Pillar"))
            {
                Debug.Log("Ran into pillar");
                this.numCollisions++;
            }

            this.comboMove = AttkEffect.None;
        }

        public int GetNumCollisions()
        {
            return this.numCollisions;
        }
    }
}
