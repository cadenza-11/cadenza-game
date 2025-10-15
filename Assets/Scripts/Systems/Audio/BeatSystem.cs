// Implementation taken from "Perfect Beat Tracking in Unity" FMOD forum post by
// bloo_regard_q_kazoo (https://qa.fmod.com/t/perfect-beat-tracking-in-unity/18788/27).

using UnityEngine;
using System;
using System.Runtime.InteropServices;
using FMODUnity;
using System.Globalization;
using FMOD.Studio;
using UnityEngine.UI;
using TMPro;

namespace Cadenza
{
    /// <summary>
    /// Communicates with the FMOD engine to perform Unity logic synced to an audio track.
    /// Handles detection of musical beats and estimations of FMOD engine latency.
    /// </summary>
    public class BeatSystem : ApplicationSystem
    {
        private static BeatSystem singleton;

        #region Inspector Variables

        [SerializeField] private EventReference globalTrackReference;

        [Header("Options")]

        /// <summary>
        /// How far into the beat an upbeat be defined.
        /// 50% (.5) means an upbeat should occur exactly at the middle
        /// of two beats. 66% (.66) means an upbeat should occur 2/3 of
        /// the way between two beats.
        /// </summary>
        [SerializeField, Range(0f, 1f)]
        private float swingPercent = 1 / 2f;

        [Header("Debug")]
        [SerializeField] private bool doDebugSounds = false;
        [SerializeField] private StudioEventEmitter downbeatDebugSound;
        [SerializeField] private StudioEventEmitter upbeatDebugSound;
        [SerializeField] private Slider playerPhaseSlider;
        [SerializeField] private TMP_Text accuracyText;

        #endregion
        #region Events

        public delegate void BeatEventDelegate();
        public static event BeatEventDelegate BeatPlayed;
        public static event BeatEventDelegate UpBeatPlayed;

        public delegate void TempoUpdateDelegate(float beatInterval);
        public static event TempoUpdateDelegate TempoChanged;

        public delegate void MarkerListenerDelegate(string markerName);
        public static event MarkerListenerDelegate MarkerPassed;

        #endregion
        #region Public Static Variables

        public static bool PlayDebugSounds
        {
            get => singleton.doDebugSounds;
            set => singleton.doDebugSounds = value;
        }

        #endregion
        #region Private Variables

        /// <summary>
        /// The constant number of samples-per-second that the system operates at.
        /// Use this to convert samples to seconds and vice versa.
        /// </summary>
        private int sampleRate;

        /// <summary>
        /// The number of samples that have passed since system start.
        /// </summary>
        private ulong currentSamplesDSP;

        /// <summary>
        /// The number of seconds that have passed since system start.
        /// </summary>
        private double currentTimeDSP = 0f;

        /// <summary>
        /// The last updated timestamp, in seconds.
        /// </summary>
        private double previousTimeDSP = 0f;

        /// <summary>
        /// The DSP time that the current track started at, in seconds
        /// </summary>
        private double trackStartTimeDSP;

        /// <summary>
        /// The track-local timestamp (in seconds) at which the last-played upbeat occurred.
        /// </summary>
        private double lastUpBeatTime = -2;

        /// <summary>
        /// The track-local timestamp (in seconds) at which the last-played beat occurred.
        /// </summary>
        private double lastBeatTime = -2;

        /// <summary>
        /// The DSP-global timestamp (in seconds) at which the last-played beat occurred.
        /// </summary>
        private double lastBeatTimeDSP = -2;

        /// <summary>
        /// The number of seconds that pass between each beat in the current tempo.
        /// (Equivalent to 60 seconds / BPM)
        /// </summary>
        private double beatInterval = 0f;

        /// <summary>
        /// The number of seconds that have passed since the last DSP clock update.
        /// </summary>
        private double deltaTimeDSP = 0f;

        /// <summary>
        /// A configurable amount of time (in seconds) that the system should
        /// detect the beat as being earlier or later than estimated.
        /// </summary>
        private float offsetTime = 0f;

        private bool wasMarkerPassedThisFrame = false;
        private int markerTime;

        private EventInstance globalTrack;

        #endregion
        #region FMOD Variables

        private PLAYBACK_STATE globalPlayState;
        private PLAYBACK_STATE lastGlobalPlayState;
        private TimelineInfo timelineInfo = null;
        private GCHandle timelineHandle;
        private EVENT_CALLBACK beatCallback;
        private FMOD.ChannelGroup channelGroup;

        #endregion
        #region Application Callbacks

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            this.SetGlobalTrack(this.globalTrackReference);
            this.PlayGlobalTrack();
        }

        public override void OnApplicationStop()
        {
            this.timelineHandle.Free();
        }

        public override void OnUpdate()
        {
            // Check if the track has just started playing from a stopped state.
            // Also update the current play state.
            this.CheckForTrackStarted();

            if (this.globalPlayState != PLAYBACK_STATE.PLAYING)
                return;

            // Update timing values.
            this.globalTrack.getTimelinePosition(out this.timelineInfo.currentPosition);
            this.UpdateDSPClock();

            // Check for tempo change. If so, update tempo.
            this.CheckForTempoChange();

            // Check if a timeline marker was passed. If so, update beat times.
            this.CheckForMarkerHit();

            // Check if the next beat passed. If so, trigger a beat.
            this.CheckForNextBeat();
        }

        #endregion
        #region Public Static Methods

        /// <summary>
        /// Shifts the beat system's estimated DSP time forwards or backwards a certain interval.
        /// A positive offset will shift the estimated beat to be later than expected.
        /// A negative offset will shift the estimated beat to be earlier than expected.
        /// </summary>
        /// <param name="offset">How much time (in milliseconds) to shift the time estimation.</param>
        public static void SetDSPOffset(int offsetMs)
        {
            float offset = Mathf.Repeat(offsetMs / 1000f, (float)singleton.beatInterval);
            singleton.offsetTime = offset;
        }


        public static float GetAccuracy()
        {
            // singleton.UpdateDSPClock();

            // How many seconds are we into the track?
            float currentTrackTime = (float)(singleton.currentTimeDSP - singleton.trackStartTimeDSP) - singleton.offsetTime;

            // How many seconds away from the beat are we?
            float beatInterval = (float)singleton.beatInterval;
            float beatPhase = Mathf.Repeat(currentTrackTime, beatInterval);

            // How far are we from the beat, as a fraction?
            // (0 = exactly on beat, 1/2 = exactly off beat, 1 = exactly on next beat)
            float swingPercentage = beatPhase / beatInterval;

            // Map percentage -> accuracy:
            // On beat:     0 -> 1
            // Late:        (0, 0.5) -> (1, 0)
            // Off beat:    0.5 -> 0
            // Early:       (0.5, 1) -> (0, -1)
            // On next beat: 1 -> -1
            float accuracy = 1 - (2 * swingPercentage);
            singleton.playerPhaseSlider.value = Math.Abs(accuracy);
            singleton.accuracyText.text = accuracy < 0 ? "Early" : "Late";

            Debug.Log($"Track time: {currentTrackTime} / Phase: {beatPhase:F2}/{beatInterval} / Swing: {swingPercentage:P0} / accuracy: {accuracy:P0}");

            return accuracy;
        }

        #endregion
        #region Callback Methods

        [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
        private static FMOD.RESULT BeatEventCallback(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
        {
            // Retrieve the user data
            EventInstance instance = new(instancePtr);
            FMOD.RESULT result = instance.getUserData(out IntPtr timelineInfoPtr);
            if (result != FMOD.RESULT.OK)
            {
                Debug.LogError("Timeline Callback error: " + result);
                return result;
            }

            // Get the object to store beat and marker details
            GCHandle timelineHandle = GCHandle.FromIntPtr(timelineInfoPtr);
            TimelineInfo timelineInfo = (TimelineInfo)timelineHandle.Target;

            // Update the timeline's beat info.
            if (type == EVENT_CALLBACK_TYPE.TIMELINE_BEAT)
            {
                var parameter = (TIMELINE_BEAT_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(TIMELINE_BEAT_PROPERTIES));
                timelineInfo.currentBar = parameter.bar;
                timelineInfo.currentBeat = parameter.beat;
                timelineInfo.beatPosition = parameter.position;
                timelineInfo.currentTempo = parameter.tempo;
            }

            // Update the timeline's marker info.
            else if (type == EVENT_CALLBACK_TYPE.TIMELINE_MARKER)
            {
                var parameter = (TIMELINE_MARKER_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(TIMELINE_MARKER_PROPERTIES));
                timelineInfo.lastMarkerName = parameter.name;
                singleton.markerTime = parameter.position;
                singleton.CheckHitSpecialMarker(parameter.name);

                singleton.wasMarkerPassedThisFrame = true;
            }

            return FMOD.RESULT.OK;
        }

        private void OnTrackStarted()
        {
            // Reset DSP time.
            this.previousTimeDSP = 0f;
            this.UpdateDSPClock();

            this.trackStartTimeDSP = this.currentTimeDSP;
            this.lastBeatTimeDSP = this.currentTimeDSP;
            this.lastBeatTime = 0f;
            this.lastUpBeatTime = 0f;
        }

        private void OnFixedBeat()
        {
            // Debug.Log($"Calculated time of beat: {this.lastBeatTime}sec \nFMOD time of beat: {this.timelineInfo.beatPosition / 1000f}sec \nDiff: {(float)this.lastBeatTime - (this.timelineInfo.beatPosition / 1000f)}");

            // Notify systems.
            ApplicationController.PlayBeat();
            BeatPlayed?.Invoke();

            // Play debug sound.
            if (this.doDebugSounds && this.downbeatDebugSound != null)
                this.downbeatDebugSound.Play();
        }

        private void OnUpBeat()
        {
            // Notify systems.
            UpBeatPlayed?.Invoke();

            // Play debug sound.
            if (this.doDebugSounds && this.upbeatDebugSound != null)
                this.upbeatDebugSound.Play();
        }

        #endregion
        #region Private Methods

        /// <summary>
        /// "Warms up" the global track by loading its sample data.
        /// Stops any currently-playing track and unloads its sample data.
        /// </summary>
        private void SetGlobalTrack(EventReference track)
        {
            EventDescription description;

            // Stop any existing track.
            if (this.globalPlayState == PLAYBACK_STATE.PLAYING)
            {
                this.globalTrack.getDescription(out description);
                description.unloadSampleData();
                this.globalTrack.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            }

            // Load the track.
            this.globalTrack = RuntimeManager.CreateInstance(track);
            this.globalTrack.getDescription(out description);
            description.loadSampleData();

            // Setup beat callbacks.
            this.timelineInfo = new TimelineInfo();
            this.beatCallback = new EVENT_CALLBACK(BeatEventCallback);
            this.timelineHandle = GCHandle.Alloc(this.timelineInfo, GCHandleType.Pinned);
            this.globalTrack.setUserData(GCHandle.ToIntPtr(this.timelineHandle));
            this.globalTrack.setCallback(this.beatCallback, EVENT_CALLBACK_TYPE.TIMELINE_BEAT | EVENT_CALLBACK_TYPE.TIMELINE_MARKER);

            // Store song length.
            description.getLength(out this.timelineInfo.trackLength);

            // Store sample rate.
            RuntimeManager.CoreSystem.getSoftwareFormat(out this.sampleRate, out _, out _);
        }

        /// <summary>
        /// Plays the global track. Must be preceded by a call to <see cref="SetGlobalTrack"/>
        /// </summary>
        private void PlayGlobalTrack()
        {
            if (!this.globalTrack.isValid())
            {
                Debug.LogWarning("BeatManager: Attempted to play an unloaded global track. Try calling SetGlobalTrack() first.");
                return;
            }
            this.globalTrack.start();
        }

        private void UpdateDSPClock()
        {
            // Get the current number of samples.
            this.globalTrack.getChannelGroup(out this.channelGroup);
            this.channelGroup.getDSPClock(out this.currentSamplesDSP, out _);

            // Calculate the current DSP time in seconds.
            this.previousTimeDSP = this.currentTimeDSP;
            this.currentTimeDSP = (double)this.currentSamplesDSP / this.sampleRate;
            this.deltaTimeDSP = this.currentTimeDSP - this.previousTimeDSP;
        }

        /// <summary>
        /// Detects if the FMOD track tempo changed this frame, and adjusts DSP accordingly.
        /// </summary>
        private bool CheckForTempoChange()
        {
            bool shouldSetTempo = this.timelineInfo.currentTempo != this.timelineInfo.previousTempo;
            if (shouldSetTempo)
                this.SetTrackTempo();

            return shouldSetTempo;
        }

        /// <summary>
        /// Detects if a beat boundary has been passed this frame.
        /// </summary>
        private bool CheckForNextBeat()
        {
            // How many seconds are we into the track?
            float currentTrackTime = (float)(this.currentTimeDSP - this.trackStartTimeDSP) - this.offsetTime;

            // How many seconds away from the beat are we?
            float beatPhase = Mathf.Repeat(currentTrackTime, (float)this.beatInterval);

            bool beatBoundaryPassed = currentTrackTime >= this.lastBeatTime + this.beatInterval;
            if (beatBoundaryPassed)
            {
                this.lastBeatTime = currentTrackTime - beatPhase;
                this.lastBeatTimeDSP = this.currentTimeDSP - beatPhase;
                this.OnFixedBeat();
            }

            return beatBoundaryPassed;
        }

        /// <summary>
        /// Detects if an up-beat boundary has been passed this frame.
        /// </summary>
        private bool CheckForNextUpBeat()
        {
            // How many seconds are we into the track?
            float currentTrackTime = (float)(this.currentTimeDSP - this.trackStartTimeDSP) - this.offsetTime;

            // How many seconds will we be into the track, one upbeat from now?
            float upBeatPosition = (float)(currentTrackTime + this.beatInterval * this.swingPercent);

            // How many seconds away from the upbeat are we?
            float upbeatPhase = Mathf.Repeat(currentTrackTime, (float)this.beatInterval);

            bool upbeatBoundaryPassed = upBeatPosition >= this.lastUpBeatTime + this.beatInterval;
            if (upbeatBoundaryPassed)
            {
                this.lastUpBeatTime = upBeatPosition - upbeatPhase;
                this.OnUpBeat();
            }

            return upbeatBoundaryPassed;
        }

        /// <summary>
        /// Detects if an FMOD timeline marker has been passed this frame.
        /// </summary>
        private bool CheckForMarkerHit()
        {
            if (!this.wasMarkerPassedThisFrame)
                return false;

            this.wasMarkerPassedThisFrame = false;

            // If the last beat is more than a half-beat old,
            if (this.lastBeatTimeDSP < this.currentTimeDSP - (this.beatInterval / 2f))
                this.OnFixedBeat();

            this.globalTrack.getTimelinePosition(out int currentTimelinePos);
            float offset = (currentTimelinePos - this.markerTime) / 1000f;

            this.trackStartTimeDSP = this.currentTimeDSP - offset;
            this.lastBeatTime = 0f;
            this.lastBeatTimeDSP = this.trackStartTimeDSP;
            this.lastUpBeatTime = 0f;

            MarkerPassed?.Invoke(this.timelineInfo.lastMarkerName);
            return true;
        }

        /// <summary>
        /// Detects if the track has changed from a "not-playing" to a "playing" state this frame.
        /// Also updates the play state.
        /// </summary>
        private bool CheckForTrackStarted()
        {
            this.globalTrack.getPlaybackState(out this.globalPlayState);

            bool trackStartedThisFrame =
                this.globalPlayState == PLAYBACK_STATE.PLAYING &&
                this.lastGlobalPlayState != PLAYBACK_STATE.PLAYING;

            if (trackStartedThisFrame)
                this.OnTrackStarted();

            this.lastGlobalPlayState = this.globalPlayState;
            return trackStartedThisFrame;
        }

        private void SetTrackTempo()
        {
            this.globalTrack.getTimelinePosition(out int currentTimelinePos);

            float offset = (currentTimelinePos - this.timelineInfo.beatPosition) / 1000f;

            this.trackStartTimeDSP = this.currentTimeDSP - offset;
            this.lastBeatTime = 0f;
            this.lastBeatTimeDSP = this.trackStartTimeDSP;
            this.lastUpBeatTime = 0f;

            this.timelineInfo.previousTempo = this.timelineInfo.currentTempo;
            this.beatInterval = 60f / this.timelineInfo.currentTempo;

            TempoChanged?.Invoke((float)this.beatInterval);
        }

        /// <summary>
        /// Detects if a marker has a special name that should perform some function
        /// (e.g. "swing=value").
        /// </summary>
        /// <param name="markerName">The name of the marker as defined in FMOD.</param>
        private void CheckHitSpecialMarker(string markerName)
        {
            string[] tokens = markerName.ToLower().Split('=', StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length != 2)
                return;

            string specifier = tokens[0].Trim();
            float.TryParse(tokens[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out float value);

            switch (specifier)
            {
                case "swing":
                    this.SetSwingPercent(value);
                    break;
            }
        }

        private void SetSwingPercent(float swingPercent)
        {
            this.swingPercent = swingPercent;
        }

        #endregion
    }
}
