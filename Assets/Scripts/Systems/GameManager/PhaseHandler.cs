using System;
using UnityEngine;

namespace Cadenza
{
    public class PhaseHandler
    {
        private Phase? currentPhase;
        public Phase? CurrentPhase => this.currentPhase;
        public event Action<Phase> PhaseExited;
        public event Action<Phase> PhaseEntered;

        private Level currentLevel;

        #region Application Callbacks

        public void OnGameStart()
        {
            BeatSystem.MarkerPassed += this.OnMarkerPassed;
            this.SetLevel(ApplicationController.CurrentLevel);
        }

        public void OnGameStop()
        {
            this.SetLevel(null);
            BeatSystem.MarkerPassed += this.OnMarkerPassed;
        }

        private void OnMarkerPassed(string markerName)
        {
            foreach (var phase in this.currentLevel.Phases)
            {
                if (string.Equals(markerName, phase.FMODMarkerName))
                {
                    this.EnterPhase(phase);
                    return;
                }
            }
        }

        #endregion
        #region Public Methods

        public bool RequestNextPhase()
        {
            int index = Array.IndexOf(this.currentLevel.Phases, this.currentPhase);

            if (index == -1)
                return false;
            if (index == this.currentLevel.Phases.Length - 1)
                return false;

            if (this.currentPhase != null)
                this.ExitPhase(this.currentPhase.Value);

            AudioSystem.SetParameter("MusicPhase", index + 1);
            return true;
        }

        #endregion
        #region Private Methods

        private void SetLevel(Level level)
        {
            this.currentLevel = level;
            this.currentPhase = null;
        }

        private void ExitPhase(Phase phase)
        {
            Debug.Log($"Exited music phase '{phase.FMODMarkerName}' (index={Array.IndexOf(this.currentLevel.Phases, this.currentPhase)})");
            this.PhaseExited?.Invoke(phase);
        }

        private void EnterPhase(Phase phase)
        {
            this.currentPhase = phase;
            Debug.Log($"Entered music phase '{phase.FMODMarkerName}' (index={Array.IndexOf(this.currentLevel.Phases, this.currentPhase)})");
            this.PhaseEntered?.Invoke(phase);
        }

        #endregion
    }
}
