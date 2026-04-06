using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            Backstage,
            Stage,
            Combat,
            Results,
        }

        private const string ParamNameState = "GameState";
        private const string ParamNameStage = "Stage";

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

        [SerializeField] private EventReference pregameMusicEvent;
        [SerializeField] private EventReference beatCallbackDebugEvent;
        [SerializeField] private EventReference playerOneShotsEvent;
        [SerializeField] private SoundCollection soundCollection;

        public static EventReference PlayerOneShotsEvent => singleton.playerOneShotsEvent;
        private HashSet<AudioEvent> beatSetOneShot;
        private PlayerSounds playerSounds;
        private UISounds uiSounds;

        private Bus masterBus;
        private Bus musicBus;
        private Bus sfxBus;

        private FMODAudioVisualizer audioVisualizer;
        public static float[] FFTSpectrum => singleton.audioVisualizer?.mFFTSpectrum;
        public const int FFTWindowSize = FMODAudioVisualizer.WindowSize;

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

            // Play pregame music track.
            BeatSystem.PlayTrack(this.pregameMusicEvent);
        }

        public override void OnApplicationStop()
        {
            this.audioVisualizer?.OnApplicationStop();
        }

        public override void OnUpdate()
        {
            this.audioVisualizer?.OnUpdate();
        }

        public override void OnGameStart()
        {
            _ = this.OnGameStartAsync();

            this.playerSounds?.OnGameStart();
            this.uiSounds?.OnGameStart();
        }

        public override void OnGameStop()
        {
            _ = this.OnGameStopAsync();
            this.playerSounds?.OnGameStop();
            this.uiSounds?.OnGameStop();
        }

        private async Task OnGameStartAsync()
        {
            // Stop level music if redirecting from pregame menus.
            if (ApplicationController.PreviousLevel == null)
            {
                BeatSystem.StopTrack();
                await BeatSystem.WaitForTrackStop();
            }

            // Play the music for the selected level, if not null.
            if (GameManager.SelectedLevel is Level level && !level.MusicTrack.IsNull)
                BeatSystem.PlayTrack(level.MusicTrack);
        }

        private async Task OnGameStopAsync()
        {
            // If there is no level queued up, stop current
            // music and start the pregame music track.
            if (ApplicationController.CurrentLevel == null)
            {
                BeatSystem.StopTrack();
                await BeatSystem.WaitForTrackStop();

                BeatSystem.PlayTrack(this.pregameMusicEvent);
            }
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

        public static void PlayOneShot(Sound.Gameplay sound, bool immediate = false)
        {
            var eventRef = singleton.soundCollection.Get(sound);
            if (!eventRef.IsNull)
                PlayOneShot(eventRef, immediate);
        }

        public static void PlayOneShot(Sound.UI sound, bool immediate = false)
        {
            var eventRef = singleton.soundCollection.Get(sound);
            if (!eventRef.IsNull)
                PlayOneShot(eventRef, immediate);
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

        public static void SetState(State state)
        {
            Debug.Log($"Audio state set to {state}");
            RuntimeManager.StudioSystem.setParameterByName(ParamNameState, (int)state);
        }

        public static void SetParameter(string parameterName, bool enabled)
        {
            RuntimeManager.StudioSystem.setParameterByName(parameterName, enabled ? 1 : 0);
        }

        public static void SetParameter(string parameterName, float value)
        {
            RuntimeManager.StudioSystem.setParameterByName(parameterName, value);
        }

        public static float Lin2dB(float linear)
        {
            return Mathf.Clamp(Mathf.Log10(linear) * 20.0f, -80.0f, 0.0f);
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
            this.OnBanksLoaded();
        }

        private void OnBanksLoaded()
        {
            // Set up audio busses.
            this.masterBus = RuntimeManager.GetBus("bus:/Master");
            this.musicBus = RuntimeManager.GetBus("bus:/Master/Music");
            this.sfxBus = RuntimeManager.GetBus("bus:/Master/SFX");

            this.playerSounds = new();
            this.playerSounds.Initialize();

            this.uiSounds = new();
            this.uiSounds.Initialize();

            this.audioVisualizer = new();
            this.audioVisualizer.OnInitialize(this.musicBus);

            Debug.Log("Loaded all banks from FMOD.");
        }

        #endregion
    }
}
