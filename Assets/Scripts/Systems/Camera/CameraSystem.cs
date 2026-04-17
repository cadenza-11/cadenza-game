using Unity.Cinemachine;
using UnityEngine;

namespace Cadenza
{
    public class CameraSystem : ApplicationSystem
    {
        private static CameraSystem singleton;

        [Header("Camera Shake")]
        [SerializeField] private CinemachineImpulseSource ambientImpulseSource;

        private float ambientImpulseForce;
        public static float AmbientImpulseForce
        {
            get => singleton.ambientImpulseForce;
            set => singleton.ambientImpulseForce = value;
        }

        private CinemachineBrain cinemachineBrain;
        public static CinemachineBrain CinemachineBrain => singleton.cinemachineBrain;

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            this.cinemachineBrain = this.GetComponent<CinemachineBrain>();

            this.ambientImpulseForce = 0.5f;
            BeatSystem.BeatPlayed += this.OnBeatPlayed;
        }

        private void OnBeatPlayed()
        {
            this.ambientImpulseSource.GenerateImpulseWithForce(this.ambientImpulseForce);
        }
    }
}
