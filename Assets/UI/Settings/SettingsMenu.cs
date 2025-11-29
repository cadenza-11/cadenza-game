using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class SettingsMenu : UIPanel
    {

        [SerializeField] private UIDocument uiDocument;

        private VisualElement blinker;
        private Button[] tabButtons;
        private VisualElement[] tabViews;
        private int tabIndex = -1;

        #region System Events
        public override void OnInitialize()
        {
            // Set up UI.
            this.root = (TemplateContainer)this.uiDocument.rootVisualElement;
            this.root.style.display = DisplayStyle.None;

            // Configure tab buttons.
            var tabButtons = this.root.Q<VisualElement>("tab-buttons");
            this.tabButtons = new Button[tabButtons.childCount];
            for (int i = 0; i < tabButtons.childCount; i++)
            {
                int index = i; // cache loop index for closure
                this.tabButtons[i] = tabButtons.ElementAt(i) as Button;
                this.tabButtons[i].clicked += () => this.SwitchToTab(index);
            }

            // Configure tab views.
            var tabViews = this.root.Q<VisualElement>("tab-views");
            this.tabViews = new VisualElement[tabViews.childCount];
            for (int i = 0; i < tabViews.childCount; i++)
            {
                this.tabViews[i] = tabViews.ElementAt(i);
                this.tabViews[i].style.display = DisplayStyle.None;
            }

            // Configure back button.
            this.root.Q<Button>("b_Back").clicked += this.Hide;

            // Configure general.
            this.root.Q<Button>("b_DeleteSaveData").clicked += () => SaveSystem.DeletePreviousRuns();

            // Configure calibration.
            var latencySlider = this.root.Q<SliderInt>("slider_Latency");
            latencySlider.RegisterValueChangedCallback(evt => BeatSystem.SetOffset(evt.newValue));
            this.root.Q<Button>("b_SaveCalibration").clicked += () => BeatSystem.SetOffset(latencySlider.value);
            this.blinker = this.root.Q<VisualElement>("icon_Blinker");

            this.SwitchToTab(0);
            this.Hide();
        }

        public override void Show()
        {
            base.Show();
            BeatSystem.BeatPlayed += () => this.blinker.ToggleInClassList("blink");
            this.root.style.display = DisplayStyle.Flex;
        }

        public override void Hide()
        {
            base.Hide();
            BeatSystem.BeatPlayed -= () => this.blinker.ToggleInClassList("blink");
            this.root.style.display = DisplayStyle.None;
        }

        #endregion
        #region Navigation Events

        private void SwitchToTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= this.tabViews.Length)
                return;

            if (this.tabIndex >= 0 && this.tabIndex < this.tabViews.Length)
            {
                this.tabViews[this.tabIndex].style.display = DisplayStyle.None;
                this.tabButtons[this.tabIndex].ToggleInClassList("selected");
            }

            this.tabIndex = tabIndex;
            this.tabViews[this.tabIndex].style.display = DisplayStyle.Flex;
            this.tabButtons[this.tabIndex].ToggleInClassList("selected");
        }

        #endregion
        #region Private Functions

        // Container updates

        #endregion
    }
}
