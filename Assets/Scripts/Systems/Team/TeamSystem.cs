using System;
using System.Collections;
using UnityEngine;

namespace Cadenza
{
    public class Team
    {
        public string Name;
        public Guid Guid;
    }

    /// <summary>
    /// Manages gameplay logic for groups of character classes.
    /// </summary>
    public class TeamSystem : ApplicationSystem
    {
        private static TeamSystem singleton;

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            this.isCharacterFlowing = new(singleton.availableClasses.Values.Length);

            SaveSystem.TeamFileDeleted += DeleteTeam;
        }

        public override void OnGameStop()
        {
            this.ClearFlowState();
        }

        #region Team Creation

        private const string DefaultTeamName = "Unnamed Team";

        public static Team Team => team;
        public static string TeamName => team?.Name ?? DefaultTeamName;
        private static Team team = null;

        public static Team CreateTeam(string name)
        {
            // Create default team.
            team = new Team()
            {
                Name = string.IsNullOrEmpty(name) ? DefaultTeamName : name,
                Guid = Guid.NewGuid()
            };
            SaveSystem.SaveTeamToFile(team);
            SetTeam(team);
            return team;
        }

        public static void SetTeam(Team team)
        {
            TeamSystem.team = team;
        }

        public static void DeleteTeam()
        {
            team = null;
        }

        #endregion
        #region Flow Management

        [SerializeField] private CharacterSet availableClasses;
        public static CharacterSet AvailableClasses => singleton.availableClasses;
        private BitArray isCharacterFlowing;
        private int flowingCount;

        public static void SetClassFlowing(int classID, bool isFlowing)
        {
            if (classID < 0 || classID >= singleton.isCharacterFlowing.Length)
                return;

            if (singleton.isCharacterFlowing[classID] == isFlowing)
                return;

            singleton.isCharacterFlowing[classID] = isFlowing;
            singleton.flowingCount += isFlowing ? 1 : -1;
            singleton.UpdateChromaticAberration();
        }

        public static bool IsClassFlowing(int classID)
        {
            if (classID < 0 || classID >= singleton.isCharacterFlowing.Length)
                return false;

            return singleton.isCharacterFlowing[classID];
        }

        private void UpdateChromaticAberration()
        {
            // Increase chromatic aberration intensity by
            // the number of characters currently flowing.
            float targetValue = Mathf.Clamp01((float)this.flowingCount / PlayerSystem.PlayerCount);
            CameraSystem.ChromaticAberration = targetValue;
        }

        private void ClearFlowState()
        {
            this.isCharacterFlowing?.SetAll(false);
            this.flowingCount = 0;
            this.UpdateChromaticAberration();
        }

        #endregion
    }
}
