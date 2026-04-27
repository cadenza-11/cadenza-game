using UnityEngine;


namespace Cadenza
{
    public class BurstAttackVFX : MonoBehaviour
    {
        private GameObject go = null;
        [SerializeField] private GameObject burst;

        public void SetActive(bool enabled)
        {
            if (this.go == null)
                this.go = this.gameObject;

            this.go.SetActive(enabled);
        }

        private void OnTriggerEnter(Collider collider)
        {
            // Hit enemy.
            this.burst.SetActive(true);
            this.burst.transform.position = collider.transform.position;
        }

        private void OnDisable()
        {
            this.burst.SetActive(false);
        }
    }
}
