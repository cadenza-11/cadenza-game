using DG.Tweening;
using UnityEngine;

namespace Cadenza
{
    public class DJCore : MonoBehaviour
    {
        [SerializeField] private Light dedicatedSpotlight;
        private int maxLightIntensity = 1000;
        [SerializeField] private GameObject electricity;
        [SerializeField] private float punchStrength;

        private int maxHealth;
        private int currentHealth;
        private bool isDestroyed;
        private Material healthIndicatorOutline;

        public int Health => this.currentHealth;
        public bool IsDestroyed => this.isDestroyed;

        void Start()
        {
            this.healthIndicatorOutline = this.GetComponent<Renderer>().materials[1];
        }

        void OnDestroy()
        {
            BeatSystem.BeatPlayed -= this.PlayPunch;
        }

        public void Initialize(int health)
        {
            this.dedicatedSpotlight.DOIntensity(this.maxLightIntensity, 0.5f);
            this.maxHealth = health;
            this.OnHealthChanged(health);
            this.electricity.SetActive(false);
            this.isDestroyed = false;

            BeatSystem.BeatPlayed += this.PlayPunch;
        }

        public void TakeDamage(int damage)
        {
            this.OnHealthChanged(Mathf.Max(this.currentHealth - damage, 0));
            if (this.currentHealth == 0)
                this.OnDestroyed();
        }

        public void OnHealthChanged(int newHealth)
        {
            this.currentHealth = newHealth;
            this.healthIndicatorOutline.SetFloat("_HealthPercent", (float)this.currentHealth / (float)this.maxHealth);
        }

        public void OnDestroyed()
        {
            this.isDestroyed = true;
            this.dedicatedSpotlight.DOIntensity(0, 0.5f);
            this.electricity.SetActive(true);
            this.transform.DOShakePosition(1f, strength: new Vector3(0.2f, 0f, 0.2f), vibrato: 10);

            BeatSystem.BeatPlayed -= this.PlayPunch;
        }

        private void PlayPunch()
        {
            if (this.IsDestroyed) return;
            this.transform.DOPunchScale(new Vector3(this.punchStrength, 0, this.punchStrength), duration: 0.1f, vibrato: 5, elasticity: 0.5f);
        }
    }
}
