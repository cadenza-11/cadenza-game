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
        }

        public override void OnBeat()
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
            // Register for player hit events.
            PlayerSystem.PlayerHit += this.OnPlayerHit;
            ScoreSystem.TeamHit += this.OnTeamHit;

            // Set up audio busses.
            this.masterBus = RuntimeManager.GetBus("bus:/Master");
            this.musicBus = RuntimeManager.GetBus("bus:/Master/Music");
            this.sfxBus = RuntimeManager.GetBus("bus:/Master/SFX");

            Debug.Log("Loaded all banks from FMOD.");
        }

        private void OnPlayerHit(ScoreDef def)
        {
            PlayOneShotWithParameter(PlayerOneShotsEvent, "ID", 0, immediate: true);
        }

        private void OnTeamHit(TeamScoreDef def)
        {
            int soundID = def.Class switch
            {
                ScoreClass.Bad => 0,
                ScoreClass.OK => 0,
                ScoreClass.Great => 1,
                ScoreClass.Perfect => 2,
                _ => 0,
            };

            if (soundID != 0)
                PlayOneShotWithParameter(PlayerOneShotsEvent, "ID", soundID, immediate: true);
        }


        #endregion
    }
}
