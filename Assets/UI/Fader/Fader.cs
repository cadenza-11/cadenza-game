using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class Fader : UIPanel
    {
        private static Fader singleton;

        private VisualElement fader;
        private const string SlideInClassName = "slide-in";
        private const string SlideOutClassName = "slide-out";
        private const float DefaultLingerDuration = 0.5f; // seconds

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            this.fader = this.uiDocument.rootVisualElement.Q<VisualElement>("fader");
            this.Show();
        }

        /// <summary>
        /// Transitions the fader to a visible state.
        /// </summary>
        public static async Task ShowAsync()
        {
            // Do nothing if panel is already visible.
            if (singleton.fader.ClassListContains(SlideInClassName))
                return;

            var transitionCompletion = new TaskCompletionSource<bool>();

            // Transition.
            singleton.fader.style.display = DisplayStyle.Flex;
            singleton.fader.RegisterCallbackOnce<TransitionEndEvent>(_ => transitionCompletion.TrySetResult(true));
            singleton.fader.AddToClassList(SlideInClassName);

            AudioSystem.SetParameter("isFading", true);

            // Wait until transition completes.
            await transitionCompletion.Task;
        }

        /// <summary>
        /// Transitions the fader to an invisible state.
        /// </summary>
        public static async Task HideAsync()
        {
            // Panel must already be visible.
            if (!singleton.fader.ClassListContains(SlideInClassName))
                return;

            // Wait for audio to transition out.
            await BeatSystem.WaitForNextBeatAsync();
            {
                AudioSystem.SetParameter("isFading", false);
            }
            await BeatSystem.WaitForMarkerAsync("Menu");

            // Transition.
            var transitionCompletion = new TaskCompletionSource<bool>();
            singleton.fader.RegisterCallbackOnce<TransitionEndEvent>(_ => transitionCompletion.TrySetResult(true));
            singleton.fader.AddToClassList(SlideOutClassName);

            await transitionCompletion.Task;

            singleton.Reset();
        }

        private void Reset()
        {
            this.fader.style.display = DisplayStyle.None;
            this.fader.RemoveFromClassList(SlideInClassName);
            this.fader.RemoveFromClassList(SlideOutClassName);
        }
    }
}
