using UnityEngine;
using DG.Tweening;

namespace Cadenza
{
    public class ElectrifiedQuadrants : MonoBehaviour
    {
        [System.Serializable]
        private class Quadrant
        {
            public Renderer Renderer;
            public GameObject DamageCollider;
            public int ConsecutiveActivations = 0;
        }

        [SerializeField] private Quadrant[] quadrants;
        private bool isActive;
        private int beatsTillActive = 10;
        private int beatCounter = 0;
        private bool isSecondQuadrantActive;
        Color defaultColor = Color.black;

        void OnDestroy()
        {
            BeatSystem.BeatPlayed -= this.OnBeat;
        }

        public void Initialize(int cooldownBeats, bool isPhaseThree = false)
        {
            this.beatsTillActive = cooldownBeats;
            this.isSecondQuadrantActive = isPhaseThree;
            this.isActive = true;
            BeatSystem.BeatPlayed += this.OnBeat;
            this.defaultColor = this.quadrants[0].Renderer.material.color;
            foreach (var quadrant in this.quadrants)
            {
                quadrant.ConsecutiveActivations = 0;
                this.ToggleActiveEffect(quadrant, false);
            }
        }

        public void ShutDown()
        {
            foreach (var quadrant in this.quadrants)
                this.ToggleActiveEffect(quadrant, false);

            this.isActive = false;
            BeatSystem.BeatPlayed -= this.OnBeat;
        }

        private void OnBeat()
        {
            if (!this.isActive) return;
            this.beatCounter++;
            if (this.beatCounter == this.beatsTillActive - 4)
            {
                this.SelectQuadrants();
                for (int i = 0; i < 4; i++)
                {
                    if (this.quadrants[i].ConsecutiveActivations > 0)
                        this.quadrants[i].Renderer.material.DOColor(Color.paleGoldenRod, 0.2f);
                }
                return;
            }

            if (this.beatCounter == this.beatsTillActive)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (this.quadrants[i].ConsecutiveActivations > 0)
                        this.ToggleActiveEffect(this.quadrants[i], true);
                }
            }
            if (this.beatCounter >= this.beatsTillActive && this.beatCounter <= this.beatsTillActive + 7)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (this.quadrants[i].ConsecutiveActivations > 0)
                        this.quadrants[i].DamageCollider.SetActive(!this.quadrants[i].DamageCollider.activeSelf);
                }
            }

            if (this.beatCounter == this.beatsTillActive + 8)
            {
                this.beatCounter = 0;
                foreach (var quadrant in this.quadrants)
                    this.ToggleActiveEffect(quadrant, false);
            }
        }

        private void ToggleActiveEffect(Quadrant quadrant, bool isActive)
        {
            Debug.Log($"Setting quadrant {quadrant} active: {isActive}");
            quadrant.Renderer.material.DOColor(isActive ? Color.yellow : this.defaultColor, 0.2f);
            quadrant.DamageCollider.SetActive(isActive);
        }

        private void SelectQuadrants()
        {
            if (this.isSecondQuadrantActive) // Pick two quadrants
            {
                int firstQuadrant = this.RandomizeSelection(2);
                int secondQuadrant = this.RandomizeSelection(2, firstQuadrant);
                for (int i = 0; i < 4; i++)
                {
                    if (i == firstQuadrant || i == secondQuadrant)
                        this.quadrants[i].ConsecutiveActivations++;
                    else
                        this.quadrants[i].ConsecutiveActivations = 0;
                }
                return;
            }

            int chosenQuadrant = this.RandomizeSelection(1);
            for (int i = 0; i < 4; i++)
            {
                    if (i == chosenQuadrant)
                        this.quadrants[i].ConsecutiveActivations++;
                    else
                        this.quadrants[i].ConsecutiveActivations = 0;
            }
        }

        private int RandomizeSelection(int maxConsecutive, int otherSelectedQuadrant = -1)
        {
            int chosenQuadrant = -1;
            while (chosenQuadrant == -1) // Pick one quadrant
            {
                int rand = Random.Range(0, 4);
                if (this.quadrants[rand].ConsecutiveActivations < maxConsecutive && rand != otherSelectedQuadrant)
                    chosenQuadrant = rand;
            }
            return chosenQuadrant;
        }
    }
}

