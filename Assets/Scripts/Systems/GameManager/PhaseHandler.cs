using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cadenza
{
    public class PhaseHandler
    {
        private Dictionary<string, Phase> phasesByMarkerName = new();
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
            if (this.phasesByMarkerName.TryGetValue(markerName, out Phase phase))
                this.EnterPhase(phase);
        }

        #endregion
        #region Public Methods

        public bool RequestNextPhase()
        {
            int nextPhaseIndex = this.currentPhase == null ? 0 : this.currentPhase.Value.Index + 1;
            if (nextPhaseIndex >= this.currentLevel.Phases.Length - 1)
                return false;

            if (this.currentPhase != null)
                this.ExitPhase(this.currentPhase.Value);

            AudioSystem.SetParameter("MusicPhase", nextPhaseIndex);
            return true;
        }

        #endregion
        #region Private Methods

        private void SetLevel(Level level)
        {
            this.currentLevel = level;
            this.currentPhase = null;
            this.phasesByMarkerName.Clear();

            if (this.currentLevel != null)
            {
                foreach (var phase in this.currentLevel.Phases)
                    this.phasesByMarkerName[phase.FMODMarkerName] = phase;
            }
        }

        private void ExitPhase(Phase phase)
        {
            Debug.Log($"Exited music phase '{phase.FMODMarkerName}'");
            this.PhaseExited?.Invoke(phase);
        }

        private void EnterPhase(Phase phase)
        {
            if (phase == this.currentPhase)
                return;

            this.currentPhase = phase;
            Debug.Log($"Entered music phase '{phase.FMODMarkerName}'");
            this.PhaseEntered?.Invoke(phase);
        }

        #endregion
    }
}
