using System.Collections.Generic;
using UnityEngine;

namespace Cadenza
{
    public class DoDamage : MonoBehaviour
    {
        [SerializeField] private int damageAmount;
        [SerializeField] private bool continuousDamage;
        private List<Collider> playersInRange = new List<Collider>();
        private int beatCounter = 0;

        void Awake()
        {
            BeatSystem.BeatPlayed += this.OnBeat;
        }

        void OnDestroy()
        {
            BeatSystem.BeatPlayed -= this.OnBeat;
        }

        private void OnTriggerEnter(Collider collider)
        {
            Debug.Log($"Dropping vinyl collided with {collider.gameObject.name}");
            if (collider.CompareTag("Player"))
            {
                this.playersInRange.Add(collider);
                Character hitEntity = collider.gameObject.GetComponent<Character>();
                if (hitEntity != null)
                    hitEntity.TakeDamage(this.damageAmount);
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            if (collider.CompareTag("Player"))
                this.playersInRange.Remove(collider);
        }

        private void OnBeat()
        {
            this.beatCounter++;
            if (!this.continuousDamage || this.beatCounter % 4 != 0) return;
            foreach (var collider in this.playersInRange)
            {
                Character hitEntity = collider.gameObject.GetComponent<Character>();
                if (hitEntity != null)
                    hitEntity.TakeDamage(this.damageAmount);
            }
        }
    }
}