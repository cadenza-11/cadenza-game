using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Cadenza
{
    [UxmlElement]
    public partial class OnScreenKeyboard : VisualElement, INotifyValueChanged<string>
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const int DefaultMaxCharacterLimit = 12;
        private const int DefaultKeysPerRow = 9;
        private const string FocusedKeyClass = "is-focused";

        private readonly Label inputElement;
        private readonly VisualElement keysContainer;
        private readonly VisualElement actionButtonsContainer;
        private readonly List<Button> letterButtons = new();
        private readonly List<Button> keyButtons = new();
        private readonly Button deleteButton;
        private readonly Button spaceButton;
        private readonly Button cancelButton;
        private readonly Button submitButton;

        private string currentValue = string.Empty;
        private int maxCharacterLimit = DefaultMaxCharacterLimit;
        private int keysPerRow = DefaultKeysPerRow;
        private int focusedKeyIndex = -1;

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

        [UxmlAttribute("keys-per-row")]
        public int KeysPerRow
        {
            get => this.keysPerRow;
            set => this.keysPerRow = Mathf.Max(1, value);
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
        public IReadOnlyList<Button> KeyButtons => this.keyButtons;
        public Button DeleteButton => this.deleteButton;
        public Button CancelButton => this.cancelButton;
        public Button SubmitButton => this.submitButton;
        public int FocusedKeyIndex => this.focusedKeyIndex;

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
                int buttonIndex = this.keyButtons.Count;
                var keyButton = new Button(() => this.AppendCharacter(letter))
                {
                    name = $"key_{letter}",
                    text = letter.ToString(),
                    focusable = false,
                    userData = letter
                };

                keyButton.AddToClassList("on-screen-keyboard__key");
                this.letterButtons.Add(keyButton);
                this.keyButtons.Add(keyButton);
                this.keysContainer.Add(keyButton);

                keyButton.clicked += () => this.SetFocusedKeyIndex(buttonIndex);
            }

            int spaceButtonIndex = this.keyButtons.Count;
            this.spaceButton = new Button(() => this.AppendCharacter(' '))
            {
                name = "key_space",
                focusable = false,
            };
            this.spaceButton.AddToClassList("on-screen-keyboard__key");
            this.spaceButton.AddToClassList("on-screen-keyboard__key--space");
            this.keyButtons.Add(this.spaceButton);
            this.keysContainer.Add(this.spaceButton);
            this.spaceButton.clicked += () => this.SetFocusedKeyIndex(spaceButtonIndex);

            int deleteButtonIndex = this.keyButtons.Count;
            this.deleteButton = new Button(this.DeleteLastCharacter)
            {
                name = "key_delete",
                focusable = false,
            };
            this.deleteButton.AddToClassList("on-screen-keyboard__key");
            this.deleteButton.AddToClassList("on-screen-keyboard__key--delete");
            this.keyButtons.Add(this.deleteButton);
            this.keysContainer.Add(this.deleteButton);
            this.deleteButton.clicked += () => this.SetFocusedKeyIndex(deleteButtonIndex);

            this.actionButtonsContainer = new VisualElement();
            this.actionButtonsContainer.style.flexDirection = FlexDirection.Row;
            this.actionButtonsContainer.style.justifyContent = Justify.SpaceAround;
            this.actionButtonsContainer.style.marginTop = 20;

            this.cancelButton = new Button
            {
                text = "Cancel",
                name = "b_CancelName",
                focusable = false,
            };
            this.cancelButton.style.flexGrow = 1;
            this.keyButtons.Add(this.cancelButton);
            int cancelButtonIndex = this.keyButtons.Count - 1;
            this.cancelButton.clicked += () => this.SetFocusedKeyIndex(cancelButtonIndex);

            this.submitButton = new Button
            {
                text = "Submit",
                name = "b_SubmitName",
                focusable = false,
            };
            this.submitButton.style.flexGrow = 1;
            this.keyButtons.Add(this.submitButton);
            int submitButtonIndex = this.keyButtons.Count - 1;
            this.submitButton.clicked += () => this.SetFocusedKeyIndex(submitButtonIndex);

            this.actionButtonsContainer.Add(this.cancelButton);
            this.actionButtonsContainer.Add(this.submitButton);
            this.hierarchy.Add(this.actionButtonsContainer);

            this.RefreshInputElement();
            this.SetFocusedKeyIndex(0);
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

        public void OnSubmit()
        {
            if (this.focusedKeyIndex == -1)
                return;

            Button focusedButton = this.keyButtons[this.focusedKeyIndex];

            if (focusedButton == this.deleteButton)
                this.DeleteLastCharacter();
            else if (focusedButton == this.spaceButton)
                this.AppendCharacter(' ');
            else if (focusedButton == this.cancelButton || focusedButton == this.submitButton)
                InvokeButtonSubmit(focusedButton);
            else
                this.AppendCharacter((char)focusedButton.userData);
        }

        public void OnCancel()
        {
            this.DeleteLastCharacter();
        }

        public void OnNavigate(MoveDirection direction)
        {
            if (direction == MoveDirection.None || this.keyButtons.Count == 0)
                return;

            int currentIndex = Mathf.Max(0, this.focusedKeyIndex);
            int nextIndex = this.GetNextFocusIndex(currentIndex, direction);
            this.SetFocusedKeyIndex(nextIndex);
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

        private int GetNextFocusIndex(int currentIndex, MoveDirection direction)
        {
            int actionRowStart = this.GetActionRowStartIndex();
            int keyGridLastIndex = actionRowStart - 1;
            int maxIndex = this.keyButtons.Count - 1;
            bool inActionRow = currentIndex >= actionRowStart;
            int rowStart = inActionRow ? actionRowStart : (currentIndex / this.keysPerRow) * this.keysPerRow;
            int rowEnd = inActionRow ? maxIndex : Mathf.Min(rowStart + this.keysPerRow - 1, keyGridLastIndex);
            int column = currentIndex - rowStart;

            switch (direction)
            {
                case MoveDirection.Left:
                    return currentIndex > rowStart ? currentIndex - 1 : currentIndex;

                case MoveDirection.Right:
                    return currentIndex < rowEnd ? currentIndex + 1 : currentIndex;

                case MoveDirection.Up:
                    if (inActionRow)
                    {
                        if (keyGridLastIndex < 0)
                            return currentIndex;

                        int lastGridRowStart = (keyGridLastIndex / this.keysPerRow) * this.keysPerRow;
                        int lastGridRowEnd = keyGridLastIndex;
                        int targetColumn = Mathf.Clamp(column, 0, lastGridRowEnd - lastGridRowStart);
                        return lastGridRowStart + targetColumn;
                    }

                    return currentIndex - this.keysPerRow >= 0
                        ? currentIndex - this.keysPerRow
                        : currentIndex;

                case MoveDirection.Down:
                    if (inActionRow)
                        return currentIndex;

                    int nextIndex = currentIndex + this.keysPerRow;
                    if (nextIndex <= keyGridLastIndex)
                        return nextIndex;

                    if (actionRowStart > maxIndex)
                        return currentIndex;

                    int actionRowLength = maxIndex - actionRowStart + 1;
                    int actionColumn = Mathf.Clamp(column, 0, actionRowLength - 1);
                    return actionRowStart + actionColumn;

                default:
                    return currentIndex;
            }
        }

        private int GetActionRowStartIndex()
        {
            // Letters + delete + space form the keyboard grid;
            // cancel and submit are in the action row.
            return this.letterButtons.Count + 2;
        }

        private static void InvokeButtonSubmit(Button button)
        {
            using (NavigationSubmitEvent submitEvent = NavigationSubmitEvent.GetPooled())
            {
                submitEvent.target = button;
                button.SendEvent(submitEvent);
            }
        }

        private void SetFocusedKeyIndex(int index)
        {
            if (this.keyButtons.Count == 0)
            {
                this.focusedKeyIndex = -1;
                return;
            }

            int nextIndex = Mathf.Clamp(index, 0, this.keyButtons.Count - 1);
            if (this.focusedKeyIndex == nextIndex)
                return;

            if (this.focusedKeyIndex >= 0 && this.focusedKeyIndex < this.keyButtons.Count)
                this.keyButtons[this.focusedKeyIndex].RemoveFromClassList(FocusedKeyClass);

            this.focusedKeyIndex = nextIndex;
            Button focusedButton = this.keyButtons[this.focusedKeyIndex];
            focusedButton.AddToClassList(FocusedKeyClass);
        }
    }
}
