using System;
using UnityEngine;

namespace Cadenza
{
    public class Team
    {
        public string Name;
        public Guid Guid;
    }

    public static class TeamSystem
    {
        private const string DefaultTeamName = "Unnamed Team";

        public static Team Team => team;
        public static string TeamName => team?.Name;
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
    }
}
