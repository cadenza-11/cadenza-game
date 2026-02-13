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
            if (ApplicationController.State != ApplicationState.Pregame)
            {
                Debug.LogWarning("Cannot delete the current team in the current state. Delete the team in a pregame menu.");
                return;
            }

            team = null;
            SaveSystem.DeleteTeamFile();
        }

        #endregion
        #region Flow Management

        [SerializeField] private CharacterSet availableClasses;
        public static CharacterSet AvailableClasses => singleton.availableClasses;
        private static readonly BitArray isCharacterFlowing = new(singleton.availableClasses.Values.Length);

        public static void SetClassFlowing(int classID, bool isFlowing)
        {
            if (classID < 0 || classID >= isCharacterFlowing.Length)
                return;

            isCharacterFlowing[classID] = isFlowing;
        }

        public static bool IsClassFlowing(int classID)
        {
            if (classID < 0 || classID >= isCharacterFlowing.Length)
                return false;

            return isCharacterFlowing[classID];
        }

        #endregion
    }
}
