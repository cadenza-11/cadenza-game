using UnityEngine;

namespace Cadenza
{
    [RequireComponent(typeof(Enemy))]
    public sealed class EnemyHealthParameterBinding : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Global FMOD parameter to drive with this enemy's normalized health.")]
        public string parameterName;

        private Enemy enemy;
        private float lastSentValue = float.NaN;
        private string lastSentParameterName;

        private void Awake()
        {
            this.enemy = this.GetComponent<Enemy>();
        }

        private void OnEnable()
        {
            this.lastSentValue = float.NaN;
            this.lastSentParameterName = null;
            this.SyncParameter();
        }

        private void LateUpdate()
        {
            this.SyncParameter();
        }

        private void SyncParameter()
        {
            if (string.IsNullOrWhiteSpace(this.parameterName))
                return;

            float health01 =
                this.enemy == null ? 0f :
                this.enemy.GetMaxHealth() <= 0f ? 0f : Mathf.Clamp01(this.enemy.CurrentHealth / this.enemy.GetMaxHealth());

            if (this.lastSentParameterName == this.parameterName &&
                Mathf.Approximately(this.lastSentValue, health01))
            {
                return;
            }

            BeatSystem.CurrentTrack.setParameterByName(this.parameterName, health01);
            this.lastSentValue = health01;
            this.lastSentParameterName = this.parameterName;
        }
    }
}
