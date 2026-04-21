using UnityEngine;

namespace Cadenza
{
    /// <summary>
    /// Syncs an Animator to BeatSystem events.
    /// - Optional trigger firing on each beat/upbeat.
    /// - Optional state restart on each beat.
    /// - Optional tempo-based speed retiming so a loop spans N beats.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class BeatSyncedAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;

        [Header("Beat Events")]
        [SerializeField] private bool listenForBeat = true;
        [SerializeField] private bool listenForUpBeat;

        [Tooltip("Animator trigger fired when a listened beat event occurs. Leave empty to disable.")]
        [SerializeField] private string beatTriggerName = "OnBeat";

        [Header("State Re-Sync")]
        [Tooltip("If enabled, the target animator state restarts at normalized time 0 when a beat event occurs.")]
        [SerializeField] private bool restartStateOnBeat;
        [SerializeField] private string stateName = "Idle";
        [SerializeField, Min(0)] private int stateLayer;

        [Header("Tempo Sync")]
        [Tooltip("If enabled, animator speed is retimed from BeatSystem tempo.")]
        [SerializeField] private bool retimeAnimatorSpeed = true;

        [Tooltip("How many beats should one animation loop take.")]
        [SerializeField, Min(0.25f)] private float beatsPerLoop = 1f;

        [Tooltip("Length of the base animation at animator speed=1.")]
        [SerializeField, Min(0.01f)] private float baseLoopLengthSeconds = 1f;

        private AnimationClip currentClip => this.animator.GetCurrentAnimatorClipInfo(0)[0].clip;
        private int beatTriggerHash;
        private int stateHash;

        private void Reset()
        {
            this.animator = this.GetComponent<Animator>();
        }

        private void Awake()
        {
            if (this.animator == null)
                this.animator = this.GetComponent<Animator>();

            this.CacheHashes();
        }

        private void OnEnable()
        {
            if (this.listenForBeat)
                BeatSystem.BeatPlayed += this.OnBeatEvent;
            if (this.listenForUpBeat)
                BeatSystem.UpBeatPlayed += this.OnBeatEvent;

            BeatSystem.TempoChanged += this.OnTempoChanged;
            this.ApplyTempoSync();
        }

        private void OnDisable()
        {
            BeatSystem.BeatPlayed -= this.OnBeatEvent;
            BeatSystem.UpBeatPlayed -= this.OnBeatEvent;
            BeatSystem.TempoChanged -= this.OnTempoChanged;
        }

        private void OnValidate()
        {
            this.beatsPerLoop = Mathf.Max(0.25f, this.beatsPerLoop);
            this.baseLoopLengthSeconds = Mathf.Max(0.01f, this.baseLoopLengthSeconds);

            this.CacheHashes();
        }

        private void OnTempoChanged(float bpm)
        {
            this.ApplyTempoSync();
        }

        private void OnBeatEvent()
        {
            if (this.animator == null)
                return;

            if (!string.IsNullOrWhiteSpace(this.beatTriggerName))
                this.animator.SetTrigger(this.beatTriggerHash);

            if (this.restartStateOnBeat && !string.IsNullOrWhiteSpace(this.stateName))
                this.animator.Play(this.stateHash, this.stateLayer, 0f);
        }

        private void ApplyTempoSync()
        {
            if (!this.retimeAnimatorSpeed || this.animator == null)
                return;

            double secondsPerBeat = BeatSystem.SecondsPerBeat;
            if (secondsPerBeat <= 0d)
                return;

            float targetLoopLength = (float)secondsPerBeat * this.beatsPerLoop;
            if (targetLoopLength <= 0f)
                return;

            this.animator.speed = this.currentClip.length / targetLoopLength;
        }

        private void CacheHashes()
        {
            this.beatTriggerHash = Animator.StringToHash(this.beatTriggerName);
            this.stateHash = Animator.StringToHash(this.stateName);
        }
    }
}
