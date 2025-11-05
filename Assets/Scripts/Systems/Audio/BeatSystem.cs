// Implementation taken from "Perfect Beat Tracking in Unity" FMOD forum post by
// bloo_regard_q_kazoo (https://qa.fmod.com/t/perfect-beat-tracking-in-unity/18788/27).

using UnityEngine;
using System;
using System.Runtime.InteropServices;
using FMODUnity;
using System.Globalization;
using FMOD.Studio;
using Unity.Mathematics;

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

        #endregion
        #region Events

        public delegate void BeatEventDelegate();
        public static event BeatEventDelegate BeatPlayed;
        public static event BeatEventDelegate UpBeatPlayed;

        public delegate void TempoUpdateDelegate(float bpm);
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

        /// <summary>
        /// The number of seconds that pass between each beat in the current tempo.
        /// (Equivalent to 60 seconds / BPM)
        /// </summary>
        public static double SecondsPerBeat => singleton.beatPeriod;

        /// <summary>
        /// The number of seconds that have elapsed in the current track.
        /// </summary>
        public static double CurrentTrackTime = singleton.elapsedTimeDSP - singleton.trackStartTimeDSP + singleton.offsetTime;

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
        private ulong elapsedSamplesDSP = 0;

        /// <summary>
        /// The number of seconds that have passed since system start.
        /// </summary>
        private double elapsedTimeDSP = 0;

        /// <summary>
        /// The last updated timestamp, in seconds.
        /// </summary>
        private double previousTimeDSP = 0;

        /// <summary>
        /// The DSP time that the current track started at, in seconds
        /// </summary>
        private double trackStartTimeDSP = 0;

        /// <summary>
        /// The track-local timestamp (in seconds) at which the last-played upbeat occurred.
        /// </summary>
        private double previousUpBeatTime = -2;

        /// <summary>
        /// The track-local timestamp (in seconds) at which the last-played beat occurred.
        /// </summary>
        private double previousBeatTime = -2;

        /// <summary>
        /// The DSP-global timestamp (in seconds) at which the last-played beat occurred.
        /// </summary>
        private double previousBeatTimeDSP = -2;

        /// <summary>
        /// The number of seconds that pass between each beat in the current tempo.
        /// (Equivalent to 60 seconds / BPM)
        /// </summary>
        private double beatPeriod = 0f;

        /// <summary>
        /// The number of seconds that have passed since the last DSP clock update.
        /// </summary>
        private double deltaTimeDSP = 0;

        /// <summary>
        /// A configurable amount of time (in ms) that the system should
        /// detect the beat as being earlier or later than estimated.
        /// </summary>
        private double systemOffsetTime = 0;

        /// <summary>
        /// The amount of latency (in ms) it takes to communicate to/from FMOD.
        /// </summary>
        private const double FMODOffsetTime = 0;

        /// <summary>
        /// The total amount of latency (in seconds) to configure the beat detection system for.
        /// </summary>
        private double offsetTime => (this.systemOffsetTime + FMODOffsetTime) / 1000;

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
        /// Shifts the beat system's estimated time forwards or backwards a certain interval.
        /// A positive offset will shift the estimated beat to be later than expected.
        /// A negative offset will shift the estimated beat to be earlier than expected.
        /// </summary>
        /// <param name="offset">How much time (in milliseconds) to shift the time estimation.</param>
        public static void SetOffset(int offsetMs)
        {
            float offset = Mathf.Repeat(offsetMs, (float)singleton.beatPeriod * 1000);
            singleton.systemOffsetTime = offset;
        }

        /// <summary>
        /// Calculates how far (in seconds) the given timestamp is from the closest beat.
        /// The sign of the value indicates if it was earlier or later than the closest beat
        /// (negative means early, positive means late).
        /// </summary>
        public static double GetLatency(double timestamp)
        {
            singleton.UpdateDSPClock();

            double beatPeriod = singleton.beatPeriod;
            double beatPhase = Cadenza.Utils.Math.Repeat(timestamp, beatPeriod);
            double distance = math.min(beatPhase, beatPeriod - beatPhase);
            int sign = (beatPhase <= beatPeriod / 2) ? +1 : -1;

            return sign * distance;
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
            this.SetTrackStart(offset: 0);
        }

        private void OnFixedBeat()
        {
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
            this.channelGroup.getDSPClock(out this.elapsedSamplesDSP, out _);

            // Calculate the current DSP time in seconds.
            this.previousTimeDSP = this.elapsedTimeDSP;
            this.elapsedTimeDSP = (double)this.elapsedSamplesDSP / this.sampleRate;
            this.deltaTimeDSP = this.elapsedTimeDSP - this.previousTimeDSP;
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
            double elapsedTrackTime = this.elapsedTimeDSP - this.trackStartTimeDSP + this.offsetTime;
            double beatPeriod = Cadenza.Utils.Math.Repeat(elapsedTrackTime, this.beatPeriod);

            bool beatBoundaryPassed = elapsedTrackTime >= this.previousBeatTime + this.beatPeriod;
            if (beatBoundaryPassed)
            {
                this.previousBeatTime = elapsedTrackTime - beatPeriod;
                this.previousBeatTimeDSP = this.elapsedTimeDSP - beatPeriod;
                this.OnFixedBeat();
            }

            return beatBoundaryPassed;
        }

        /// <summary>
        /// Detects if an up-beat boundary has been passed this frame.
        /// </summary>
        private bool CheckForNextUpBeat()
        {
            double elapsedTrackTime = this.elapsedTimeDSP - this.trackStartTimeDSP + this.offsetTime;
            double estimatedUpbeatTime = elapsedTrackTime + this.beatPeriod * this.swingPercent;
            double beatPeriod = Cadenza.Utils.Math.Repeat(elapsedTrackTime, (float)this.beatPeriod);

            bool upbeatBoundaryPassed = estimatedUpbeatTime >= this.previousUpBeatTime + this.beatPeriod;
            if (upbeatBoundaryPassed)
            {
                this.previousUpBeatTime = estimatedUpbeatTime - beatPeriod;
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

            // If the last beat is more than a half-beat old, go to the next beat.
            if (this.previousBeatTimeDSP < this.elapsedTimeDSP - (this.beatPeriod / 2f))
                this.OnFixedBeat();

            this.globalTrack.getTimelinePosition(out int currentTimelinePos);
            float offset = (currentTimelinePos - this.markerTime) / 1000f;
            this.SetTrackStart(offset);

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
            this.SetTrackStart(offset);

            this.timelineInfo.previousTempo = this.timelineInfo.currentTempo;
            this.beatPeriod = 60f / this.timelineInfo.currentTempo;

            TempoChanged?.Invoke(this.timelineInfo.currentTempo);
        }

        /// <summary>
        /// Updates the start of the track to be offsetted from the
        /// "real" track start time by the provided number of seconds.
        /// </summary>
        /// <param name="offset"></param>
        private void SetTrackStart(float offset)
        {
            this.trackStartTimeDSP = this.elapsedTimeDSP - offset;
            this.previousBeatTimeDSP = this.trackStartTimeDSP;
            this.previousBeatTime = 0f;
            this.previousUpBeatTime = 0f;
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
