// Implementation taken from "Perfect Beat Tracking in Unity" FMOD forum post by
// bloo_regard_q_kazoo (https://qa.fmod.com/t/perfect-beat-tracking-in-unity/18788/27).

using UnityEngine;
using System;
using System.Runtime.InteropServices;
using FMODUnity;
using System.Globalization;
using FMOD.Studio;

namespace Cadenza
{
    public class BeatSystem : ApplicationSystem
    {
        private static BeatSystem singleton;

        #region Public Variables
        public EventReference globalTrack;

        [Header("DEBUG:")]
        public bool doDebugSounds = false;
        public StudioEventEmitter downBeatEvent;
        public StudioEventEmitter upBeatEvent;

        [Header("OPTIONS:")]
        public float swingPercent = 1 / 2f;

        #endregion

        #region Private Static Variables
        private int sampleRate;
        private double currentSamples = 0;

        private ulong dspClock;
        private bool pendingTempoChange = false;
        private double tempoTrackDSPStartTime;
        private double lastUpBeatTime = -2;
        #endregion


        #region Events
        public delegate void BeatEventDelegate();
        public static event BeatEventDelegate OnFixedBeat;
        public static event BeatEventDelegate OnUpBeat;

        public delegate void TempoUpdateDelegate(float beatInterval);
        public static event TempoUpdateDelegate OnTempoChanged;

        public delegate void MarkerListenerDelegate(string markerName);
        public static event MarkerListenerDelegate OnMarkerPassed;
        #endregion

        #region Private Static Variables
        private static double lastFixedBeatTime = -2;
        private static double lastFixedBeatDSPTime = -2;
        private static double currentTime = 0f;
        private static float currentPitch = 1f;
        private static double beatInterval = 0f;
        private static double dspDeltaTime = 0f;
        private static double lastDSPTime = 0f;

        private static bool wasMarkerPassedThisFrame = false;
        private static int markerTime;

        private static EventInstance musicTrack;
        public static bool isPlayingMusic = false;

        #endregion

        #region FMOD Variables
        private PLAYBACK_STATE lastMusicPlayState;

        [StructLayout(LayoutKind.Sequential)]
        public class TimelineInfo
        {
            public int currentBeat = 0;
            public int currentBar = 0;
            public int beatPosition = 0;
            public float currentTempo = 0;
            public float lastTempo = 0;
            public int currentPosition = 0;
            public int songLengthInMS = 0;
            public FMOD.StringWrapper lastMarker = new FMOD.StringWrapper();
        }
        public TimelineInfo timelineInfo = null;
        private GCHandle timelineHandle;
        private EVENT_CALLBACK beatCallback;
        #endregion

        #region Application Callbacks

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            this.SetMusicTrack(this.globalTrack);
            this.PlayMusicTrack();
        }

        public override void OnUpdate()
        {
            // Update playback state.
            PLAYBACK_STATE currentState = this.UpdatePlaybackState();
            if (currentState != PLAYBACK_STATE.PLAYING)
                return;

            // Update values.
            musicTrack.getTimelinePosition(out this.timelineInfo.currentPosition);
            UpdateDSPClock();

            // Check for tempo change. If so, update tempo.
            this.CheckForTempoChange();

            // Check if a timeline marker was passed. If so, update beat times.
            this.CheckForMarkerHit();

            // Check if the next beat passed. If so, trigger a beat.
            this.CheckForNextBeat();
        }

        #endregion

        public static void SetPitch(float newPitch)
        {
            musicTrack.setPitch(newPitch);
            currentPitch = newPitch;
        }

        public static float GetPitch()
        {
            return currentPitch;
        }

        #region Public Static Methods

        public static float GetBeatInterval()
        {
            return (float)beatInterval;
        }

        public static float GetLastFixedBeatDSPTime()
        {
            return (float)lastFixedBeatDSPTime;
        }

        public static float GetCurrentTime()
        {
            return (float)currentTime;
        }

        public static int GetCurrentTimeInMilliseconds()
        {
            return singleton.timelineInfo.currentPosition;
        }

        public static int GetTrackLengthInMilliseconds()
        {
            return singleton.timelineInfo.songLengthInMS;
        }

        public static float GetDSPDeltaTime()
        {
            return (float)dspDeltaTime;
        }

        public static float GetUpBeatPosition()
        {
            return (float)beatInterval * singleton.swingPercent;
        }

        #endregion

        /// <summary>
        /// "Warms up" the music track by loading its sample data.
        /// </summary>
        public void SetMusicTrack(EventReference track)
        {
            EventDescription description;

            if (isPlayingMusic)
            {
                isPlayingMusic = false;

                musicTrack.getDescription(out description);
                description.unloadSampleData();
                musicTrack.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            }

            // Load the track.
            musicTrack = RuntimeManager.CreateInstance(track);
            musicTrack.getDescription(out description);
            description.loadSampleData();


            // Setup beat callbacks.
            singleton.timelineInfo = new TimelineInfo();
            singleton.beatCallback = new EVENT_CALLBACK(BeatEventCallback);
            singleton.timelineHandle = GCHandle.Alloc(singleton.timelineInfo, GCHandleType.Pinned);
            musicTrack.setUserData(GCHandle.ToIntPtr(singleton.timelineHandle));
            musicTrack.setCallback(singleton.beatCallback, EVENT_CALLBACK_TYPE.TIMELINE_BEAT | EVENT_CALLBACK_TYPE.TIMELINE_MARKER);

            // Store song length.
            description.getLength(out int length);
            singleton.timelineInfo.songLengthInMS = length;
            Debug.Log($"SONG LENGTH = {length}ms");

            // Store sample rate.
            RuntimeManager.CoreSystem.getSoftwareFormat(out singleton.sampleRate, out _, out _);
        }

        public void PlayMusicTrack()
        {
            if (!musicTrack.isValid())
            {
                Debug.LogWarning("BeatManager: Attempted to play an unloaded music track. Try calling SetMusicTrack() first.");
                return;
            }
            musicTrack.start();
            isPlayingMusic = true;
        }

        private void SetTrackStartInfo()
        {
            lastDSPTime = 0f;

            UpdateDSPClock();

            tempoTrackDSPStartTime = currentTime;
            lastFixedBeatTime = 0f;
            lastFixedBeatDSPTime = currentTime;
        }

        private void UpdateDSPClock()
        {
            musicTrack.getChannelGroup(out FMOD.ChannelGroup channelGroup);
            channelGroup.getDSPClock(out this.dspClock, out _);

            this.currentSamples = this.dspClock;
            currentTime = this.currentSamples / this.sampleRate;
            dspDeltaTime = currentTime - lastDSPTime;
            lastDSPTime = currentTime;
        }

        private void CheckForTempoChange()
        {
            bool shouldSetTempo = timelineInfo.currentTempo != timelineInfo.lastTempo && !pendingTempoChange;
            if (shouldSetTempo)
                SetTrackTempo();
        }

        private void CheckForNextBeat()
        {
            float fixedSongPosition = (float)(currentTime - tempoTrackDSPStartTime);
            float upBeatSongPosition = fixedSongPosition + GetUpBeatPosition();

            // FIXED BEAT
            if (fixedSongPosition >= lastFixedBeatTime + beatInterval)
            {
                float r = Mathf.Repeat(fixedSongPosition, (float)beatInterval);

                DoFixedBeat();

                lastFixedBeatTime = fixedSongPosition - r;
                lastFixedBeatDSPTime = currentTime - r;

                if (pendingTempoChange)
                {
                    SetTrackTempo();
                    pendingTempoChange = false;
                }
            }

            // UP BEAT
            if (upBeatSongPosition >= lastUpBeatTime + beatInterval)
            {
                float r = Mathf.Repeat(upBeatSongPosition, (float)beatInterval);
                DoUpBeat();
                lastUpBeatTime = upBeatSongPosition - r;
            }
        }

        private void CheckForMarkerHit()
        {
            if (!wasMarkerPassedThisFrame)
                return;

            wasMarkerPassedThisFrame = false;

            if (lastFixedBeatDSPTime < currentTime - (beatInterval / 2f))
                DoFixedBeat();

            // SetTrackTempo()?
            musicTrack.getTimelinePosition(out int currentTimelinePos);
            float offset = (currentTimelinePos - markerTime) / 1000f;

            tempoTrackDSPStartTime = currentTime - offset;
            lastFixedBeatTime = 0f;
            lastFixedBeatDSPTime = tempoTrackDSPStartTime;
            lastUpBeatTime = 0f;

            OnMarkerPassed?.Invoke(timelineInfo.lastMarker);
        }

        private PLAYBACK_STATE UpdatePlaybackState()
        {
            musicTrack.getPlaybackState(out PLAYBACK_STATE musicPlayState);

            if (musicPlayState == PLAYBACK_STATE.PLAYING &&
                this.lastMusicPlayState != PLAYBACK_STATE.PLAYING)
                SetTrackStartInfo();

            this.lastMusicPlayState = musicPlayState;
            return musicPlayState;
        }

        private void DoFixedBeat()
        {
            OnFixedBeat?.Invoke();

            if (doDebugSounds && this.downBeatEvent != null)
                this.downBeatEvent.Play();
        }

        private void DoUpBeat()
        {
            OnUpBeat?.Invoke();

            if (doDebugSounds && this.upBeatEvent != null)
                this.upBeatEvent.Play();
        }

        private void SetSwingPercent(float swingPercent)
        {
            this.swingPercent = swingPercent;
        }

        private void SetTrackTempo()
        {
            musicTrack.getTimelinePosition(out int currentTimelinePos);

            float offset = (currentTimelinePos - timelineInfo.beatPosition) / 1000f; // divided into seconds
            tempoTrackDSPStartTime = currentTime - offset;
            lastFixedBeatTime = 0f;
            lastFixedBeatDSPTime = tempoTrackDSPStartTime;
            lastUpBeatTime = 0f;
            timelineInfo.lastTempo = timelineInfo.currentTempo;
            beatInterval = 60f / timelineInfo.currentTempo;

            OnTempoChanged?.Invoke((float)beatInterval);
        }

        /// <summary>
        /// Detect if a marker has a special name that should perform some function
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

        [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
        static FMOD.RESULT BeatEventCallback(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
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
                timelineInfo.lastMarker = parameter.name;
                markerTime = parameter.position;
                singleton.CheckHitSpecialMarker(parameter.name);

                wasMarkerPassedThisFrame = true;
            }

            return FMOD.RESULT.OK;
        }
    }
}
