using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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

        private VolumeProfile globalVolumeProfile;
        private ChromaticAberration chromaticAberration;
        public static float ChromaticAberration
        {
            get => singleton.chromaticAberration.intensity.value;
            set => SetChromaticAberration(value);
        }

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            this.cinemachineBrain = this.GetComponent<CinemachineBrain>();
            this.globalVolumeProfile = GraphicsSettings
                .GetRenderPipelineSettings<URPDefaultVolumeProfileSettings>()
                .volumeProfile;

            this.globalVolumeProfile.TryGet(out this.chromaticAberration);
            this.chromaticAberration.intensity.overrideState = true;

            VolumeManager.instance.OnVolumeProfileChanged(singleton.globalVolumeProfile);

            this.ambientImpulseForce = 0.5f;
            BeatSystem.BeatPlayed += this.OnBeatPlayed;
        }

        private void OnBeatPlayed()
        {
            this.ambientImpulseSource.GenerateImpulseWithForce(this.ambientImpulseForce);
        }

        public static void SetChromaticAberration(float value)
        {
            DOTween.To(
                () => singleton.chromaticAberration.intensity.value,
                value =>
                {
                    singleton.chromaticAberration.intensity.value = value;
                    VolumeManager.instance.OnVolumeProfileChanged(singleton.globalVolumeProfile);
                },
                value,
                0.2f);

        }
    }
}
