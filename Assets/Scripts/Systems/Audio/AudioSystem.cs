using System;
using System.Collections;
using System.Collections.Generic;
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
                this.instance.start();
                this.instance.setPaused(true);
            }

            public AudioEvent(EventReference eventReference, params (string name, float value)[] parameters)
                : this(eventReference)
            {
                foreach ((string name, float value) in parameters)
                    this.instance.setParameterByName(name, value);
            }

            public bool Equals(AudioEvent other)
            {
                return this.reference.Guid == other.reference.Guid;
            }

            public override int GetHashCode()
            {
                return this.reference.Guid.GetHashCode();
            }

            public void PlayOneShot()
            {
                if (this.instance.isValid())
                {
                    this.instance.setPaused(false);
                    this.instance.release();
                }
            }
        }

        [SerializeField] private EventReference globalBeatEvent;
        [SerializeField] private EventReference beatCallbackDebugEvent;
        [SerializeField] private EventReference playerOneShotsEvent;
        public static EventReference PlayerOneShotsEvent => singleton.playerOneShotsEvent;
        private HashSet<AudioEvent> beatSetOneShot;

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            this.beatSetOneShot = new();

            // Start loading all audio banks.
            this.StartCoroutine(this.LoadAllBanks());
        }

        public override void OnBeat()
        {
            // Dump (play) all queued one-shots.
            foreach (var evt in this.beatSetOneShot)
                evt.PlayOneShot();
            this.beatSetOneShot.Clear();
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
        }

        public static void PlayOneShot(EventReference sound, bool immediate = false)
        {
            var evt = new AudioEvent(sound);

            if (immediate)
                evt.PlayOneShot();
            else
                singleton.beatSetOneShot.Add(evt);

        }

        public static void PlayOneShotWithParameter(EventReference sound, string parameterName, float value, bool immediate = false)
        {
            var evt = new AudioEvent(sound, parameters: (parameterName, value));

            if (immediate)
                evt.PlayOneShot();
            else
                singleton.beatSetOneShot.Add(evt);
        }
    }
}
