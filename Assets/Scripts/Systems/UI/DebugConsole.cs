using System;
using System.Collections.Generic;
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
        private ConsoleCommands commandParser;

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
                label.text = this.logs[i].message;
                label.style.color = this.logs[i].color;
            };
            this.listView.itemsSource = this.logs;

            // Trigger command via text field.
            this.commandParser = new();
            this.textField = this.root.Q<TextField>();
            this.textField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return)
                {
                    this.commandParser.OnCommand(this.textField.text);
                    this.textField.value = string.Empty;
                }
                else if (evt.keyCode == KeyCode.UpArrow)
                {
                    this.commandParser.OnGetPreviousCommand(this.textField);
                }
                else if (evt.keyCode == KeyCode.DownArrow)
                {
                    this.commandParser.OnGetNextCommand(this.textField);
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

        private void ToggleVisibility()
        {
            if (this.root.style.display == DisplayStyle.Flex)
            {
                this.root.style.display = DisplayStyle.None;
                InputSystem.PlayerInputMap.Enable();
            }
            else if (this.root.style.display == DisplayStyle.None)
            {
                this.root.style.display = DisplayStyle.Flex;
                InputSystem.PlayerInputMap.Enable();
            }
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
            this.listView.ScrollToItem(this.logs.Count - 1);
        }
    }
}
