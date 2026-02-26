using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    [UxmlElement]
    public partial class OnScreenKeyboard : VisualElement, INotifyValueChanged<string>
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const int DefaultMaxCharacterLimit = 12;

        private readonly Label inputElement;
        private readonly VisualElement keysContainer;
        private readonly List<Button> letterButtons = new();
        private readonly Button deleteButton;

        private string currentValue = string.Empty;
        private int maxCharacterLimit = DefaultMaxCharacterLimit;

        [UxmlAttribute("max-character-limit")]
        public int MaxCharacterLimit
        {
            get => this.maxCharacterLimit;
            set
            {
                int clamped = Mathf.Max(0, value);
                if (this.maxCharacterLimit == clamped)
                    return;

                this.maxCharacterLimit = clamped;
                this.value = this.currentValue;
            }
        }

        [UxmlAttribute("value")]
        public string Value
        {
            get => this.value;
            set => this.value = value;
        }

        public string value
        {
            get => this.currentValue;
            set
            {
                string normalized = this.NormalizeValue(value);
                if (this.currentValue == normalized)
                    return;

                using (ChangeEvent<string> changedEvent = ChangeEvent<string>.GetPooled(this.currentValue, normalized))
                {
                    changedEvent.target = this;
                    this.SetValueWithoutNotify(normalized);
                    this.SendEvent(changedEvent);
                }
            }
        }

        public Label InputElement => this.inputElement;
        public VisualElement KeysContainer => this.keysContainer;
        public IReadOnlyList<Button> LetterButtons => this.letterButtons;
        public Button DeleteButton => this.deleteButton;

        public OnScreenKeyboard()
        {
            this.AddToClassList("on-screen-keyboard");

            this.inputElement = new Label
            {
                name = "input",
            };
            this.inputElement.AddToClassList("on-screen-keyboard__input");
            this.hierarchy.Add(this.inputElement);

            this.keysContainer = new VisualElement
            {
                name = "keys",
            };
            this.keysContainer.AddToClassList("on-screen-keyboard__keys");
            this.hierarchy.Add(this.keysContainer);

            foreach (char character in Alphabet)
            {
                char letter = character;
                var keyButton = new Button(() => this.AppendCharacter(letter))
                {
                    name = $"key_{letter}",
                    text = letter.ToString(),
                };

                keyButton.AddToClassList("on-screen-keyboard__key");
                this.letterButtons.Add(keyButton);
                this.keysContainer.Add(keyButton);
            }

            this.deleteButton = new Button(this.DeleteLastCharacter)
            {
                name = "key_delete",
                text = "DELETE",
            };
            this.deleteButton.AddToClassList("on-screen-keyboard__key");
            this.deleteButton.AddToClassList("on-screen-keyboard__key--delete");
            this.keysContainer.Add(this.deleteButton);

            this.RefreshInputElement();
        }

        public void SetValueWithoutNotify(string newValue)
        {
            this.currentValue = this.NormalizeValue(newValue);
            this.RefreshInputElement();
        }

        private void AppendCharacter(char character)
        {
            if (this.currentValue.Length >= this.maxCharacterLimit)
                return;

            this.value = this.currentValue + character;
        }

        private void DeleteLastCharacter()
        {
            if (this.currentValue.Length == 0)
                return;

            this.value = this.currentValue.Substring(0, this.currentValue.Length - 1);
        }

        private string NormalizeValue(string rawValue)
        {
            string source = rawValue ?? string.Empty;
            if (source.Length == 0 || this.maxCharacterLimit == 0)
                return string.Empty;

            var filteredBuilder = new StringBuilder(this.maxCharacterLimit);
            for (int i = 0; i < source.Length; i++)
            {
                char upperChar = char.ToUpperInvariant(source[i]);
                if (upperChar < 'A' || upperChar > 'Z')
                    continue;

                filteredBuilder.Append(upperChar);
                if (filteredBuilder.Length >= this.maxCharacterLimit)
                    break;
            }

            return filteredBuilder.ToString();
        }

        private void RefreshInputElement()
        {
            this.inputElement.text = this.currentValue;
        }
    }
}