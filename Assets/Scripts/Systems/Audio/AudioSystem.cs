using System;
using System.Collections;
using System.Runtime.InteropServices;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace Cadenza
{
    public class AudioSystem : ApplicationSystem
    {
        private static AudioSystem singleton;

        private class TimelineInfo
        {
            public int currentBeat;
            public float currentTempo;
        }

        [SerializeField] private EventReference globalBeatEvent;
        [SerializeField] private EventReference beatCallbackDebugEvent;
        private EventInstance globalTrack;
        private TimelineInfo timelineInfo;

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            // Start loading all audio banks.
            this.StartCoroutine(LoadAllBanks());
        }

        private IEnumerator LoadAllBanks()
        {
            // Wait until all the audio sample data loading is done
            while (!RuntimeManager.HaveAllBanksLoaded || RuntimeManager.AnySampleDataLoading())
            {
                yield return null;
            }
            this.BanksLoaded();
        }

        private void BanksLoaded()
        {
            Debug.Log("Loaded all banks from FMOD.");

            // Start the global track.
            this.globalTrack = RuntimeManager.CreateInstance(this.globalBeatEvent.Path);
            Debug.Log("Starting global music track.");

            // Get timeline information.
            timelineInfo = new TimelineInfo();
            EVENT_CALLBACK beatCallback = new(BeatEventCallback);
            this.globalTrack.setUserData(GCHandle.ToIntPtr(GCHandle.Alloc(timelineInfo)));
            this.globalTrack.setCallback(beatCallback, EVENT_CALLBACK_TYPE.TIMELINE_BEAT);

            this.globalTrack.start();
        }

        public static void PlayOneShot(EventReference sound)
        {
            RuntimeManager.PlayOneShot(sound);
        }

        public static void PlayOneShotWithParameter(EventReference sound, string parameterName, float value)
        {
            EventInstance instance = RuntimeManager.CreateInstance(sound);

            instance.setParameterByName(parameterName, value);
            instance.start();
            instance.release();
        }

        [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
        private static FMOD.RESULT BeatEventCallback(EVENT_CALLBACK_TYPE type, IntPtr _event, IntPtr parameterPtr)
        {
            if (type == EVENT_CALLBACK_TYPE.TIMELINE_BEAT)
            {
                // Timeline properties reside in the beatParam variable
                var beatParam = (TIMELINE_BEAT_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(TIMELINE_BEAT_PROPERTIES));

                // Test out FMOD round-trip time by playing a test sound whenever we receieve the beat
                RuntimeManager.PlayOneShot(singleton.beatCallbackDebugEvent);
            }
            return FMOD.RESULT.OK;
        }
    }
}
