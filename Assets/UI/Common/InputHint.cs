using Cadenza.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public enum ControllerType
    {
        Keyboard,
        Xbox,
        PlayStation,
        All
    }

    [UxmlElement]
    partial class InputHint : VisualElement
    {
        private VisualElement keyboardHintElement;
        private Label keyboardSlashElement;
        private VisualElement xboxHintElement;
        private Label xboxSlashElement;
        private VisualElement psHintElement;

        private ControllerType shownControls = ControllerType.All;
        private Texture2D keyboardHint;
        private Texture2D xboxHint;
        private Texture2D psHint;
        private int hintSize = 40;
        private bool pulseToBeat;
        private bool isAttachedToPanel;
        private bool isSubscribedToBeat;

        [UxmlAttribute]
        public bool PulseToBeat
        {
            get => this.pulseToBeat;
            set
            {
                if (this.pulseToBeat == value)
                    return;

                this.pulseToBeat = value;
                this.UpdateBeatSubscription();
            }
        }

        [UxmlAttribute]
        public ControllerType ShownControls
        {
            get => this.shownControls;
            set
            {
                if (this.shownControls == value)
                    return;

                this.shownControls = value;
                this.UpdateDisplay(this.shownControls);
            }
        }

        [UxmlAttribute]
        public Texture2D KeyboardHint
        {
            get => this.keyboardHint;
            set
            {
                this.keyboardHint = value;

                this.keyboardHintElement.style.backgroundImage = this.keyboardHint;
            }
        }

        [UxmlAttribute]
        public Texture2D XboxHint
        {
            get => this.xboxHint;
            set
            {
                this.xboxHint = value;

                this.xboxHintElement.style.backgroundImage = this.xboxHint;
            }
        }

        [UxmlAttribute]
        public Texture2D PSHint
        {
            get => this.psHint;
            set
            {
                this.psHint = value;

                this.psHintElement.style.backgroundImage = this.psHint;
            }
        }

        [UxmlAttribute]
        public int HintSize
        {
            get => this.hintSize;
            set
            {
                this.hintSize = value;
                this.keyboardHintElement.style.width = this.hintSize;
                this.keyboardHintElement.style.height = this.hintSize;
                this.xboxHintElement.style.width = this.hintSize;
                this.xboxHintElement.style.height = this.hintSize;
                this.psHintElement.style.width = this.hintSize;
                this.psHintElement.style.height = this.hintSize;
            }
        }

        public InputHint()
        {
            this.style.flexDirection = FlexDirection.Row;
            this.style.alignItems = Align.Center;

            this.RegisterCallback<AttachToPanelEvent>(this.OnAttachToPanel);
            this.RegisterCallback<DetachFromPanelEvent>(this.OnDetachFromPanel);

            this.keyboardHintElement = new VisualElement();
            this.keyboardHintElement.style.backgroundImage = this.keyboardHint;
            this.keyboardHintElement.style.width = this.hintSize;
            this.keyboardHintElement.style.height = this.hintSize;

            this.keyboardSlashElement = new Label("/");

            this.xboxHintElement = new VisualElement();
            this.xboxHintElement.style.backgroundImage = this.xboxHint;
            this.xboxHintElement.style.width = this.hintSize;
            this.xboxHintElement.style.height = this.hintSize;

            this.xboxSlashElement = new Label("/");

            this.psHintElement = new VisualElement();
            this.psHintElement.style.backgroundImage = this.psHint;
            this.psHintElement.style.width = this.hintSize;
            this.psHintElement.style.height = this.hintSize;

            this.Add(this.keyboardHintElement);
            this.Add(this.keyboardSlashElement);
            this.Add(this.xboxHintElement);
            this.Add(this.xboxSlashElement);
            this.Add(this.psHintElement);

            this.UpdateDisplay(this.shownControls);
        }

        private void OnBeatPlayed()
        {
            this.PulseScale(0.2f);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            this.isAttachedToPanel = true;
            this.UpdateBeatSubscription();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            this.isAttachedToPanel = false;
            this.UpdateBeatSubscription();
        }

        private void UpdateBeatSubscription()
        {
            bool shouldSubscribe = Application.isPlaying && this.isAttachedToPanel && this.pulseToBeat;
            if (shouldSubscribe == this.isSubscribedToBeat)
                return;

            if (shouldSubscribe)
                BeatSystem.BeatPlayed += this.OnBeatPlayed;
            else
                BeatSystem.BeatPlayed -= this.OnBeatPlayed;

            this.isSubscribedToBeat = shouldSubscribe;
        }

        private void UpdateDisplay(ControllerType shownControls)
        {
            this.keyboardHintElement.style.display = (shownControls == ControllerType.Keyboard || shownControls == ControllerType.All) ? DisplayStyle.Flex : DisplayStyle.None;
            this.keyboardSlashElement.style.display = (shownControls == ControllerType.All) ? DisplayStyle.Flex : DisplayStyle.None;
            this.xboxHintElement.style.display = (shownControls == ControllerType.Xbox || shownControls == ControllerType.All) ? DisplayStyle.Flex : DisplayStyle.None;
            this.xboxSlashElement.style.display = (shownControls == ControllerType.All) ? DisplayStyle.Flex : DisplayStyle.None;
            this.psHintElement.style.display = (shownControls == ControllerType.PlayStation || shownControls == ControllerType.All) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void ShowForControllerType(ControllerType controllerType)
        {
            this.shownControls = controllerType;
            this.UpdateDisplay(controllerType);
        }
    }
}
