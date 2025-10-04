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
                default:
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