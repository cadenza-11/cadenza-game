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

        public enum Group
        {
            Master,
            Music,
            SFX
        }

        // This enum and its order are mirrored in an FMOD parameter.
        public enum State
        {
            Paused,
            Menu,
            CharacterSelect,
            Game,
        }

        private const string ParamNameState = "GameState";

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
        private PlayerSounds playerSounds;
        private EventInstance metronomeEventInstance;
        private EventInstance metronomeSnapshotInstance;

        private Bus masterBus;
        private Bus musicBus;
        private Bus sfxBus;

        #region Application Callbacks

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            this.beatSetOneShot = new();

            // Start loading all audio banks.
            this.StartCoroutine(this.LoadAllBanks());

            // Listen for beat.
            BeatSystem.BeatPlayed += this.OnBeat;
        }

        public override void OnGameStart()
        {
            this.playerSounds?.OnGameStart();
        }

        public override void OnGameStop()
        {
            this.playerSounds?.OnGameStop();
        }

        private void OnBeat()
        {
            // Dump (play) all queued one-shots.
            foreach (var evt in this.beatSetOneShot)
                evt.PlayOneShot();
            this.beatSetOneShot.Clear();
        }

        #endregion
        #region Public Static Methods

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

        public static void SetVolume(Group group, float value)
        {
            Bus bus = group switch
            {
                Group.Master => singleton.masterBus,
                Group.Music => singleton.musicBus,
                Group.SFX => singleton.sfxBus,
                _ => default
            };

            bus.setVolume(Mathf.Clamp01(value));
        }

        public static void SetInstrumentActive(CharacterClass characterClass, bool enabled)
        {
            if (characterClass != null)
                RuntimeManager.StudioSystem.setParameterByName(characterClass.Name, enabled ? 1 : 0);
        }

        public static void SetState(State state)
        {
            RuntimeManager.StudioSystem.setParameterByName(ParamNameState, (int)state);
        }

        public static void SetParameter(string parameterName, bool enabled)
        {
            RuntimeManager.StudioSystem.setParameterByName(parameterName, enabled ? 1 : 0);
        }

        #endregion
        #region Private Methods

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
            // Set up audio busses.
            this.masterBus = RuntimeManager.GetBus("bus:/Master");
            this.musicBus = RuntimeManager.GetBus("bus:/Master/Music");
            this.sfxBus = RuntimeManager.GetBus("bus:/Master/SFX");

            this.playerSounds = new();
            this.playerSounds.Initialize();

            Debug.Log("Loaded all banks from FMOD.");
        }

        #endregion
    }
}
