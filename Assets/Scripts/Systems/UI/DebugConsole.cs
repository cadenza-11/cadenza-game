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
            });

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
            string[] args = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (args.Length <= 0)
                return;

            switch (args[0])
            {
                default:
                    break;
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
