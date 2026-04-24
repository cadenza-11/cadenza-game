using UnityEngine;


namespace Cadenza
{
    public class BassAttackVFX : MonoBehaviour
    {
        private GameObject go = null;
        [SerializeField] private GameObject burst;
        [SerializeField] private Character character;

        public void SetActive(bool enabled)
        {
            if (this.go == null)
                this.go = this.gameObject;

            this.go.SetActive(enabled);
        }

        private void OnTriggerEnter(Collider collider)
        {
            // Hit enemy.
            GameObject effect = Instantiate(this.burst);
            effect.GetComponent<BurstBass>().Setup(collider.gameObject.transform, this.character);
        }
    }
}
