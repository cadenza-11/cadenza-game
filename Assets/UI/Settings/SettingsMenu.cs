using UnityEngine.UIElements;

namespace Cadenza
{
    public class SettingsMenu : UIPanel
    {
        private BeatIndicator blinker;
        private Button[] tabButtons;
        private VisualElement[] tabViews;
        private int tabIndex = -1;
        private Button backButton;

        #region System Events
        public override void OnInitialize()
        {
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
            this.backButton = this.root.Q<Button>("b_Back");
            this.backButton.clicked += this.Hide;

            // Configure general.
            this.root.Q<Button>("b_DeleteSaveData").clicked += () =>
            {
                SaveSystem.DeletePreviousRuns();
                SaveSystem.DeleteTeamFile();
            };
            this.root.Q<Toggle>("toggle_Haptics").RegisterValueChangedCallback(evt => InputSystem.SetHapticsEnabled(evt.newValue));

            // Configure audio.
            this.root.Q<Slider>("slider_Master").RegisterValueChangedCallback(evt => AudioSystem.SetVolume(AudioSystem.Group.Master, evt.newValue));
            this.root.Q<Slider>("slider_Music").RegisterValueChangedCallback(evt => AudioSystem.SetVolume(AudioSystem.Group.Music, evt.newValue));
            this.root.Q<Slider>("slider_SFX").RegisterValueChangedCallback(evt => AudioSystem.SetVolume(AudioSystem.Group.SFX, evt.newValue));

            // Configure calibration.
            var latencySlider = this.root.Q<SliderInt>("slider_Latency");
            var latencyLabel = this.root.Q<Label>("label_Latency");

            // Clamp allowed latency by the current BPM.
            BeatSystem.TempoChanged += _ =>
            {
                int beatDurationMs = (int)(BeatSystem.SecondsPerBeat * 1000 / 2);
                latencySlider.highValue = +beatDurationMs;
                latencySlider.lowValue = -beatDurationMs;
            };

            latencySlider.RegisterValueChangedCallback(evt =>
            {
                BeatSystem.SetOffset(evt.newValue);
                latencyLabel.text = $"{evt.newValue:+#;-#;0}ms";
            });
            this.blinker = this.root.Q<BeatIndicator>();

            this.SwitchToTab(0);
        }

        public override void OnShow()
        {
            AudioSystem.SetMetronomeSoloed(true);

            // TODO: move this elsewhere
            this.blinker.Start();

            this.root.Q<Button>("b_DeleteSaveData").SetEnabled(
                ApplicationController.State != ApplicationState.GameSession
                && SaveSystem.SaveFileExists);
            //

            this.backButton.Focus();
        }

        public override void OnUpdate()
        {
            this.blinker.Update();
        }

        public override void OnHide()
        {
            AudioSystem.SetMetronomeSoloed(false);

            this.blinker.Stop();
        }

        #endregion
        #region Private Methods

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
    }
}
