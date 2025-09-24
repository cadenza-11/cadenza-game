using System;
using System.Collections.Generic;
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class DebugConsole : ApplicationSystem
    {
        private struct DebugLine
        {
            public string message;
            public Color color;
        }

        [SerializeField] private UIDocument uiDocument;

        private TemplateContainer root;
        private ListView listView;
        private List<DebugLine> logs;
        private TextField textField;
        private NetworkSimulator networkSimulator;

        public override void OnInitialize()
        {
            // Set up UI.
            this.root = (TemplateContainer)this.uiDocument.rootVisualElement;

            // Show Unity logs in console.
            this.listView = this.root.Q<ListView>();
            this.logs = new();

            this.listView.makeItem = () => this.listView.itemTemplate.Instantiate();
            this.listView.bindItem = (element, i) =>
            {
                var label = element.Q<Label>();
                label.text = logs[i].message;
                label.style.color = logs[i].color;
            };
            this.listView.itemsSource = logs;

            // Trigger command via text field.
            this.textField = this.root.Q<TextField>();
            this.textField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return)
                {
                    this.OnCommand(this.textField.text);
                    this.textField.value = string.Empty;
                }
            }, TrickleDown.TrickleDown);

            // Override logger.
            Application.logMessageReceived += this.OnLogMessageReceived;
            InputSystem.UIInputMap.ToggleDebug.performed += _ => this.ToggleVisibility();
        }

        public override void OnApplicationStop()
        {
            Application.logMessageReceived -= this.OnLogMessageReceived;
        }

        private void OnCommand(string text)
        {
            if (text == null || text == string.Empty)
                return;

            string[] tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string command = tokens[0];
            string[] args = tokens.Length <= 1 ? Array.Empty<string>() : tokens[1..];

            switch (command)
            {
                case "lag":
                    this.OnCommandLag(args);
                    break;
                case "ant":
                    this.OnCommandAnticipate(args);
                    break;
                default:
                    break;
            }
        }

        private void OnCommandLag(string[] args)
        {
            if (args.Length > 0 && float.TryParse(args[0], out float timeMs))
            {
                if (this.networkSimulator == null)
                    this.networkSimulator = FindFirstObjectByType<NetworkSimulator>();

                this.networkSimulator.TriggerLagSpike(TimeSpan.FromMilliseconds(timeMs));
                Debug.Log($"Triggering lag spike of {timeMs} milliseconds.");
            }
        }

        private void OnCommandAnticipate(string[] args)
        {
            if (args.Length > 0 && int.TryParse(args[0], out int timeMs))
            {
                FMODTimelineNetworkSync.NetworkCompensationTimeMs = timeMs;
                Debug.Log($"Set FMODSync's network compensation time to {timeMs}ms.");
            }
        }

        private void ToggleVisibility()
        {
            this.root.style.display = this.root.style.display == DisplayStyle.Flex
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");

            DebugLine line = new()
            {
                message = $"[{timestamp}] {condition}",
                color = type switch
                {
                    LogType.Error => Color.red,
                    LogType.Assert => Color.white,
                    LogType.Warning => Color.yellow,
                    LogType.Log => Color.white,
                    LogType.Exception => Color.red,
                    _ => throw new NotImplementedException(),
                }
            };

            this.logs.Add(line);
            this.listView.RefreshItems();
            this.listView.ScrollToItem(logs.Count - 1);
        }
    }
}
