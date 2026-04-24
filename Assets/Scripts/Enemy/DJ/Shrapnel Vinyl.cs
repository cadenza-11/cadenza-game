using UnityEngine;

namespace Cadenza
{
    public class ShrapnelVinyl : Projectile
    {
        protected override void OnTriggerEnter(Collider collider)
        {
            if (collider.CompareTag("Player"))
            {
                Character hitEntity = collider.gameObject.GetComponent<Character>();
                if (hitEntity != null && !hitEntity.TakeDamage(this.damage))
                {
                    Destroy(this.gameObject);
                    return;
                }
                if (collider.attachedRigidbody != null)
                {
                    Vector3 direction = collider.transform.position - this.transform.position;
                    collider.attachedRigidbody.AddForce(direction.normalized * this.knockbackScale, ForceMode.Impulse);
                    Destroy(this.gameObject);
                }
            }
        }
    }
}