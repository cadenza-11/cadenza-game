using System;
using System.Collections;
using System.Collections.Generic;
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

        private struct AudioEvent : IEquatable<AudioEvent>
        {
            public EventInstance instance;
            private EventReference reference;

            public AudioEvent(EventReference eventReference)
            {
                this.reference = eventReference;
                this.instance = RuntimeManager.CreateInstance(eventReference);
                this.instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            }

            public bool Equals(AudioEvent other)
            {
                return this.reference.Guid == other.reference.Guid;
            }

            public override int GetHashCode()
            {
                return this.reference.Guid.GetHashCode();
            }
        }

        [SerializeField] private EventReference globalBeatEvent;
        [SerializeField] private EventReference beatCallbackDebugEvent;
        private EventInstance globalTrack;
        private TimelineInfo timelineInfo;
        private HashSet<AudioEvent> beatSetOneShot;

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            this.beatSetOneShot = new();

            // Start loading all audio banks.
            this.StartCoroutine(LoadAllBanks());
        }

        public override void OnBeat()
        {
            // Dump (play) all queued one-shots.
            foreach (var evt in this.beatSetOneShot)
            {
                evt.instance.start();
                evt.instance.release();
            }
            this.beatSetOneShot.Clear();
        }

        private IEnumerator LoadAllBanks()
        {
            // Wait until all the audio sample data loading is done
            while (!RuntimeManager.HaveAllBanksLoaded || RuntimeManager.AnySampleDataLoading())
            {
                yield return null;
            }
            // this.BanksLoaded();
        }

        private void BanksLoaded()
        {
            Debug.Log("Loaded all banks from FMOD.");

            // Start the global track.
            this.globalTrack = RuntimeManager.CreateInstance(this.globalBeatEvent);
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
            singleton.beatSetOneShot.Add(new AudioEvent(sound));
        }

        public static void PlayOneShotWithParameter(EventReference sound, string parameterName, float value)
        {
            var evt = new AudioEvent(sound);
            singleton.beatSetOneShot.Add(evt);
            evt.instance.setParameterByName(parameterName, value);
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
