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
            }
        }

        private void OnCommandLevel(string[] args)
        {
            if (args.Length > 0 && int.TryParse(args[0], out int sceneIndex))
            {
                _ = ApplicationController.SetSceneAsync(sceneIndex);
                Debug.Log($"Loading scene with build index {sceneIndex}.");
            }
        }
    }
}
