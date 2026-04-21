using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

namespace Cadenza
{
    public class DroppingVinyl : MonoBehaviour
    {
        [SerializeField] private Light attackSpotlight;
        private Vector3 homePosition;
        Character victim;
        bool isActive;
        bool isHoming;
        int beatsTillDrop = 10;
        int beatCounter = 0;

        void Start()
        {
            this.homePosition = this.transform.position;
        }

        void Update()
        {
            if (this.isHoming && this.victim != null)
            {
                this.transform.DOMove
                (
                    new Vector3(this.victim.transform.position.x, this.homePosition.y, this.victim.transform.position.z),
                    0.5f
                );
            }
        }

        public void Initialize(int homingLength)
        {
            this.beatsTillDrop = homingLength;
            this.isActive = true;
            this.attackSpotlight.DOIntensity(1000, 0.5f);
            BeatSystem.BeatPlayed += this.OnBeat;
            this.RestartHoming();
        }

        public void ShutDown()
        {
            this.victim = null;
            this.isActive = false;
            this.isHoming = false;
            this.transform.position = Vector3.Lerp(this.transform.position, this.homePosition, 5f);
            this.attackSpotlight.DOIntensity(0, 0.5f);
            BeatSystem.BeatPlayed -= this.OnBeat;
        }

        private void OnBeat()
        {
            if (!this.isActive) return;
            this.beatCounter++;
            if (this.beatCounter == this.beatsTillDrop) // Prepare for drop
                this.isHoming = false;
            if (this.beatCounter >= this.beatsTillDrop && this.beatCounter < this.beatsTillDrop + 3) // Warning shake window
                this.transform.DOShakeScale(0.1f, strength: new Vector3(0.2f, 0f, 0.2f), vibrato: 10, randomnessMode: ShakeRandomnessMode.Harmonic);
            if (this.beatCounter == this.beatsTillDrop + 3) // Drop
            {
                this.transform.DOMoveY(-0.8f, 0.2f).OnComplete(() => {
                    this.transform.DOShakeScale(0.1f, strength: new Vector3(0.2f, 0f, 0.2f), vibrato: 10, randomnessMode: ShakeRandomnessMode.Harmonic);
                });
            }
            if (this.beatCounter > this.beatsTillDrop + 5) // Reset for next drop
            {
                this.transform.DOMoveY(this.homePosition.y, 0.5f);
                this.RestartHoming();
            }
        }

        private void FindTarget()
        {
            // First check how many characters exist
            List<Character> validTargets = new List<Character>();
            int numFainted = 0;
            foreach (var player in PlayerSystem.Players)
            {
                if (player.Character != null)
                {
                    validTargets.Add(player.Character);
                    if (player.Character.IsFainted)
                        numFainted++;
                }
            }

            // Make sure not to try to target if all characters are fainted
            if (numFainted == PlayerSystem.Players.Length)
            {
                this.ShutDown();
                return;
            }

            // Randomly select a target from the valid characters
            Character foundTarget = null;
            while (foundTarget == null || foundTarget.IsFainted)
                foundTarget = PlayerSystem.Players[Random.Range(0, PlayerSystem.Players.Length)].Character;
            this.victim = foundTarget;
        }

        private void RestartHoming()
        {
            this.beatCounter = 0;
            this.FindTarget();
            this.isHoming = true;
        }
    }
}

