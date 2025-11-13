using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class SettingsMenu : UIPanel
    {

        [SerializeField] private UIDocument uiDocument;

        private VisualElement blinker;
        private SliderInt sliderLatency;

        #region System Events
        public override void OnInitialize()
        {
            // Set up UI.
            this.root = (TemplateContainer)this.uiDocument.rootVisualElement;
            this.root.style.display = DisplayStyle.None;

            Button buttonBack = this.root.Q<Button>("b_Back");
            buttonBack.clicked += this.Hide;

            this.sliderLatency = this.root.Q<SliderInt>("slider_Latency");
            this.sliderLatency.RegisterValueChangedCallback(this.OnLatency);
            this.blinker = this.root.Q<VisualElement>("icon_Blinker");

            this.root.style.display = DisplayStyle.None;
        }

        public override void Show()
        {
            base.Show();
            BeatSystem.BeatPlayed += () => this.blinker.ToggleInClassList("blink");
            this.root.style.display = DisplayStyle.Flex;

            Debug.LogWarning("Player not being checked. Any player can use settings");
        }

        public override void Hide()
        {
            base.Hide();
            BeatSystem.BeatPlayed -= () => this.blinker.ToggleInClassList("blink");
            this.root.style.display = DisplayStyle.None;
        }

        #endregion
        #region Navigation Events

        private void OnLatency(ChangeEvent<int> evt)
        {
            BeatSystem.SetOffset(evt.newValue);
        }

        #endregion

        #region Private Functions

        // Container updates

        #endregion
    }
}
