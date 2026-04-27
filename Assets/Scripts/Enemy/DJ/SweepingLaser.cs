using UnityEngine;
using DG.Tweening;

namespace Cadenza
{
    public class SweepingLaser : MonoBehaviour
    {
        [SerializeField] private GameObject laserPivot;
        [SerializeField] private GameObject laserBeam;
        [SerializeField] private GameObject laserPreview;
        private bool isActive;
        private int beatsTillSweep = 10;
        private int beatCounter = 0;
        private Vector3 initialRotation = new Vector3(-90, 0, 0);
        private Vector3 finalRotation = new Vector3(-90, 0, 0);

        void OnDestroy()
        {
            BeatSystem.BeatPlayed -= this.OnBeat;
        }

        public void Initialize(int cooldownBeats, int offsetBeats = 0)
        {
            this.beatsTillSweep = cooldownBeats;
            this.beatCounter = -offsetBeats; // Start counting from a negative offset to delay the first sweep
            this.isActive = true;
            BeatSystem.BeatPlayed += this.OnBeat;
        }

        public void ShutDown()
        {
            this.laserBeam.SetActive(false);
            this.laserPreview.SetActive(false);
            this.isActive = false;
            BeatSystem.BeatPlayed -= this.OnBeat;
        }

        private void OnBeat()
        {
            if (!this.isActive) return;
            this.beatCounter++;
            switch (this.beatCounter - this.beatsTillSweep)
            {
                case 0: // Prepare for preview sweep
                    this.DetermineSweep();
                    this.laserPreview.SetActive(true);
                    this.laserPreview.transform.DOLocalRotate(this.initialRotation, 0f);
                    this.laserPivot.transform.DOLocalRotate(this.initialRotation, 0.5f);
                    break;
                case 1: // Animate the preview to indicate the sweep direction
                    this.laserPreview.transform.DOLocalRotate(this.finalRotation, 2f);
                    break;
                case 3: // End preview
                    this.laserPreview.SetActive(false);
                    break;
                case 4: // Prepare for actual sweep
                    this.laserBeam.SetActive(true);
                    break;
                case 5: // Animate sweep
                    this.laserPivot.transform.DOLocalRotate(this.finalRotation, 2f);
                    break;
                case 7: // End sweep
                    this.laserBeam.SetActive(false);
                    this.beatCounter = 0;
                    break;
                default:
                    break;
            }
        }

        private void DetermineSweep()
        {
            int angleDifference = 0;
            int initialAngle = 0;
            int finalAngle = 0;
            while (angleDifference < 45) // Minimum angle difference for a noticeable sweep
            {
                initialAngle = Random.Range(-90, 90);
                finalAngle = Random.Range(-90, 90);
                angleDifference = Mathf.Abs(Mathf.Abs(finalAngle) - Mathf.Abs(initialAngle));
            }
            this.initialRotation = new Vector3(-90, initialAngle, 0);
            this.finalRotation = new Vector3(-90, finalAngle, 0);
        }
    }
}

