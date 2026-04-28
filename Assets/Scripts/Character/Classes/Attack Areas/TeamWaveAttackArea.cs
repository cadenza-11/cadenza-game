using UnityEngine;


namespace Cadenza
{
    public class TeamWaveAttackArea : MonoBehaviour
    {
        public int damage;
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
            // Hit enemy.
            if (collider.gameObject.TryGetComponent(out Enemy enemy))
            {
                enemy.TakeDamage(this.damage);

                Debug.Log("Dealt Damage");

                // Add knockback.
                Vector3 direction = collider.transform.position - this.transform.position;
                Vector3 force = direction.normalized * this.knockbackScale;
                force.y = 2f;
                collider.attachedRigidbody?.AddForce(force, ForceMode.Impulse);
            }

            // Hit core (DJ-level).
            else if (collider.gameObject.CompareTag("Core"))
            {
                collider.gameObject.GetComponent<DJCore>().TakeDamage(this.damage);
            }
        }
    }
}
