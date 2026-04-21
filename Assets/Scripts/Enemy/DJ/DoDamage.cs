using UnityEngine;

namespace Cadenza
{
    public class DoDamage : MonoBehaviour
    {
        [SerializeField] private int damageAmount;
        private void OnTriggerEnter(Collider collider)
        {
            Debug.Log($"Dropping vinyl collided with {collider.gameObject.name}");
            if (collider.CompareTag("Player"))
            {
                Character hitEntity = collider.gameObject.GetComponent<Character>();
                if (hitEntity != null)
                    hitEntity.TakeDamage(this.damageAmount);
            }
        }
    }
}