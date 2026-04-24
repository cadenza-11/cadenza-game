using UnityEngine;

namespace Cadenza
{
    public class GuitarAttackVFX : MonoBehaviour
    {
        private GameObject go = null;
        [SerializeField] private GameObject lightning;
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
            GameObject zap = Instantiate(this.lightning);
            zap.GetComponent<ElectricArcGuitar>().Setup(this.gameObject.transform, collider.gameObject.transform, this.character);
        }
    }
}
