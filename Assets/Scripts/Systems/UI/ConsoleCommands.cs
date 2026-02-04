using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class ConsoleCommands
    {
        private LinkedList<string> commandHistory = new();
        private LinkedListNode<string> curr;

        public void OnCommand(string text)
        {
            if (text == null || text == string.Empty)
                return;

            // Add command to history.
            if (this.commandHistory.First == null || this.commandHistory.First.Value != text)
                this.commandHistory.AddFirst(text);

            this.curr = null;

            // Parse the command.
            string[] tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string command = tokens[0];
            string[] args = tokens.Length <= 1 ? Array.Empty<string>() : tokens[1..];

            switch (command)
            {
                case "level":
                    this.OnCommandLevel(args);
                    break;
                case "audio":
                    this.OnCommandAudio(args);
                    break;
                case "results":
                    this.OnCommandResults(args);
                    break;
                case "team":
                    this.OnCommandTeam(args);
                    break;
                case "player":
                    this.OnCommandPlayer(args);
                    break;
                default:
                    break;
            }
        }

        public void OnGetNextCommand(TextField textField)
        {
            if (this.curr == null)
            {
                textField.value = string.Empty;
                return;
            }

            if (this.curr.Previous != null)
            {
                this.curr = this.curr.Previous;
                textField.value = this.curr.Value;
            }
            else
            {
                this.curr = null;
                textField.value = string.Empty;
            }
        }

        public void OnGetPreviousCommand(TextField textField)
        {
            if (this.curr == null)
                this.curr = this.commandHistory.First;
            else if (this.curr.Next != null)
                this.curr = this.curr.Next;

            textField.value = this.curr?.Value ?? string.Empty;
        }

        private void OnCommandAudio(string[] args)
        {
            switch (args[0])
            {
                case "offset":
                    if (int.TryParse(args[1], out int offsetMs))
                    {
                        BeatSystem.SetOffset(offsetMs);
                        Debug.Log($"Setting DSP offset to {offsetMs}ms.");
                    }
                    break;

                case "debug":
                    if (string.Equals(args[1], "on"))
                    {
                        BeatSystem.PlayDebugSounds = true;
                        Debug.Log("Turning on audio debug sounds.");
                    }
                    else if (string.Equals(args[1], "off"))
                    {
                        BeatSystem.PlayDebugSounds = false;
                        Debug.Log("Turning off audio debug sounds.");
                    }
                    break;

                case "metronome":
                    if (bool.TryParse(args[1], out bool enabled))
                        AudioSystem.SetMetronomeSoloed(enabled);
                    break;
            }
        }

        private void OnCommandLevel(string[] args)
        {
            if (args.Length > 0)
            {
                Level nextLevel = null;
                foreach (var level in ApplicationController.Levels)
                {
                    if (string.Equals(level.Name, args[0]))
                        nextLevel = level;
                }
                ApplicationController.SetLevelAsync(nextLevel);
            }
        }

        private void OnCommandPlayer(string[] args)
        {
            if (args.Length < 2
                || !int.TryParse(args[0], out int playerID)
                || !PlayerSystem.TryGetPlayerByID(playerID, out Player player))
            {
                return;
            }

            switch (args[1])
            {
                case "latency":
                    if (args.Length >= 3 && int.TryParse(args[2], out int latency))
                    {
                        ScoreSystem.ResetCalibrationDataForPlayer(player);
                        ScoreSystem.AddInputLatencyForPlayer(player, latency / 1000f);
                        Debug.Log($"Set latency for player (id={playerID}) to {latency}ms");
                    }
                    break;
            }
        }

        private void OnCommandResults(string[] args)
        {
            switch (args[0])
            {
                case "save":
                    SaveSystem.SaveRunToFile(this.GetFakeRun());
                    break;

                case "load":
                    bool success = SaveSystem.GetPreviousRuns(out Results[] results);
                    Debug.Log($"Successfully retrieved {results.Length} runs. (success={success})");
                    break;

                case "delete":
                    SaveSystem.DeletePreviousRuns();
                    break;
            }
        }
        private void OnCommandTeam(string[] args)
        {
            switch (args[0])
            {
                case "save":
                    SaveSystem.SaveTeamToFile(TeamSystem.Team);
                    break;

                case "load":
                    Team team = SaveSystem.GetTeamFromFile();
                    if (team == null)
                        Debug.Log("Failed to retrieve team from file.");
                    else
                        Debug.Log($"Retrieved team from file: name={team.Name}, id={team.Guid}");
                    break;

                case "delete":
                    SaveSystem.DeleteTeamFile();
                    break;
            }
        }

        private Results GetFakeRun()
        {
            Results results = new();
            results.AddTeamScore(ScoreClass.Bad);
            results.AddPlayerScore(1, ScoreClass.Bad);
            results.AddPlayerScore(2, ScoreClass.OK);
            results.AddPlayerScore(3, ScoreClass.Great);
            results.AddPlayerScore(4, ScoreClass.Perfect);
            results.AddPlayerScore(4, ScoreClass.Perfect);
            return results;
        }
    }
}
