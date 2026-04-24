using UnityEngine;

namespace Cadenza
{
    public class DoDamage : MonoBehaviour
    {
        [SerializeField] private int damageAmount;

        private void OnTriggerEnter(Collider collider)
        {
            if (collider.CompareTag("Player"))
            {
                Character hitEntity = collider.gameObject.GetComponent<Character>();
                if (hitEntity != null)
                    hitEntity.TakeDamage(this.damageAmount);
            }
        }
    }
}