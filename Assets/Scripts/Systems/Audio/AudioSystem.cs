using System.Collections;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace Cadenza
{
    public class AudioSystem : ApplicationSystem
    {
        [SerializeField] private EventReference MUS_Background;
        private EventInstance globalTrack;

        public override void OnInitialize()
        {
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

            // Start the global globalTrack.
            this.globalTrack = RuntimeManager.CreateInstance(this.MUS_Background.Path);
            Debug.Log("Starting global music track.");
        }

        public void PlayOneShot(EventReference sound)
        {
            RuntimeManager.PlayOneShot(sound, transform.position);
        }
    }
}
