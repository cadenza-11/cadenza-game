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
        private const string FMODFadingMarkerName = "Fading";

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            this.fader = this.uiDocument.rootVisualElement.Q<VisualElement>("fader");
            this.Show();
        }

        /// <summary>
        /// Show the fader without awaiting or affecting FMOD.
        /// </summary>
        public static void ShowImmediate()
        {
            // Do nothing if panel is already visible.
            if (singleton.fader.ClassListContains(SlideInClassName))
                return;

            singleton.fader.style.display = DisplayStyle.Flex;
            singleton.fader.AddToClassList(SlideInClassName);
        }

        /// <summary>
        /// Hide the fader without awaiting or affecting FMOD.
        /// </summary>
        public static void HideImmediate()
        {
            // Panel must already be visible.
            if (!singleton.fader.ClassListContains(SlideInClassName))
                return;

            // Transition.
            singleton.fader.RegisterCallbackOnce<TransitionEndEvent>(_ => singleton.Reset());
            singleton.fader.AddToClassList(SlideOutClassName);
        }

        /// <summary>
        /// Transitions the fader to a visible state.
        /// </summary>
        public static async Task ShowAsync(bool setAudio = true)
        {
            // Do nothing if panel is already visible.
            if (singleton.fader.ClassListContains(SlideInClassName))
                return;

            var transitionCompletion = new TaskCompletionSource<bool>();

            // Transition.
            singleton.fader.style.display = DisplayStyle.Flex;
            singleton.fader.RegisterCallbackOnce<TransitionEndEvent>(_ => transitionCompletion.TrySetResult(true));
            singleton.fader.AddToClassList(SlideInClassName);

            if (setAudio)
                AudioSystem.SetParameter("isFading", true);

            // Wait until transition completes.
            await transitionCompletion.Task;
        }

        /// <summary>
        /// Transitions the fader to an invisible state.
        /// </summary>
        public static async Task HideAsync(bool setAudio = true)
        {
            // Panel must already be visible.
            if (!singleton.fader.ClassListContains(SlideInClassName))
                return;

            // Wait for audio to transition out.
            if (setAudio)
            {
                AudioSystem.SetParameter("isFading", false);
                await WaitForNonFadingMarkerAsync();
            }

            // Transition.
            var transitionCompletion = new TaskCompletionSource<bool>();
            singleton.fader.RegisterCallbackOnce<TransitionEndEvent>(_ => transitionCompletion.TrySetResult(true));
            singleton.fader.AddToClassList(SlideOutClassName);

            await transitionCompletion.Task;

            singleton.Reset();
        }

        public static async Task WaitUntilHiddenAsync()
        {
            while (singleton.fader.style.display == DisplayStyle.Flex)
                await Task.Delay(100);
        }

        public static async Task WaitUntilVisibleAsync()
        {
            while (singleton.fader.style.display == DisplayStyle.None)
                await Task.Delay(100);
        }

        private static Task WaitForNonFadingMarkerAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            void Handler(string markerName)
            {
                if (string.Equals(markerName, FMODFadingMarkerName))
                    return;
                BeatSystem.MarkerPassed -= Handler;
                tcs.TrySetResult(true);
            }

            BeatSystem.MarkerPassed += Handler;
            return tcs.Task;
        }

        private void Reset()
        {
            this.fader.style.display = DisplayStyle.None;
            this.fader.RemoveFromClassList(SlideInClassName);
            this.fader.RemoveFromClassList(SlideOutClassName);
        }
    }
}
