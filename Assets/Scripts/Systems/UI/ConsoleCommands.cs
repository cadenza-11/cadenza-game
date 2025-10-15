using System;
using UnityEngine;

namespace Cadenza
{
    public class ConsoleCommands
    {
        public void OnCommand(string text)
        {
            if (text == null || text == string.Empty)
                return;

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

        private void OnCommandAudio(string[] args)
        {
            switch (args[0])
            {
                case "offset":
                    if (int.TryParse(args[1], out int offsetMs))
                    {
                        BeatSystem.SetDSPOffset(offsetMs);
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
