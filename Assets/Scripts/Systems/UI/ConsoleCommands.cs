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
                case "kill":
                    this.OnCommandKill(args);
                    break;
                default:
                    break;
            }
        }

        private void OnCommandKill(string[] args)
        {
            switch (args[0])
            {
                case "players":
                    foreach (var character in GameObject.FindObjectsByType<Character>(FindObjectsSortMode.None))
                        character.TakeDamage((int)character.MaxHealth);
                    break;
                case "enemies":
                    foreach (var enemy in GameObject.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
                        enemy.TakeDamage(enemy.GetMaxHealth());
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

                case "param":
                    if (args.Length != 3 || !float.TryParse(args[2], out float value))
                    {
                        Debug.Log("Usage: audio param <string name> <float value>");
                        return;
                    }
                    AudioSystem.SetParameter(args[1], value);
                    Debug.Log($"Attempting to set parameter {args[1]} to value {value}.");
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

                case "accuracy":
                    if (args.Length >= 3)
                    {
                        if (string.Equals(args[2], "on"))
                        {
                            foreach (Player playerP in PlayerSystem.Players)
                            {
                                if (playerP.Character != null)
                                    playerP.Character.GetComponentInChildren<AccuracyBar>().gameObject.SetActive(true);
                            }
                        }
                        else if (string.Equals(args[2], "off"))
                        {
                            foreach (Player playerP in PlayerSystem.Players)
                            {
                                if (playerP.Character != null)
                                    playerP.Character.GetComponentInChildren<AccuracyBar>().gameObject.SetActive(false);
                            }
                        }
                    }
                    break;
            }
        }

        private void OnCommandResults(string[] args)
        {
            switch (args[0])
            {
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
    }
}
