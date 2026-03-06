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
        private const int DefaultLingerDuration = 500; // milliseconds

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            this.fader = this.uiDocument.rootVisualElement.Q<VisualElement>("fader");
            this.root.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// Transitions the fader to a visible state.
        /// </summary>
        public static new void Show()
        {
            // Do nothing if panel is already visible.
            if (singleton.fader.ClassListContains(SlideInClassName))
                return;

            singleton.fader.style.display = DisplayStyle.Flex;
            singleton.fader.AddToClassList(SlideInClassName);
        }

        /// <summary>
        /// Transitions the fader to an invisible state.
        /// </summary>
        public static new void Hide()
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
        public static async Task ShowAsync(int durationMs = DefaultLingerDuration)
        {
            // Do nothing if panel is already visible.
            if (singleton.fader.ClassListContains(SlideInClassName))
                return;

            var transitionCompletion = new TaskCompletionSource<bool>();

            // Transition.
            singleton.fader.style.display = DisplayStyle.Flex;
            singleton.fader.RegisterCallbackOnce<TransitionEndEvent>(_ => transitionCompletion.TrySetResult(true));
            singleton.fader.AddToClassList(SlideInClassName);

            // Wait until transition completes.
            await Task.Delay(durationMs);
            await transitionCompletion.Task;
        }

        /// <summary>
        /// Transitions the fader to an invisible state.
        /// </summary>
        public static async Task HideAsync(int durationMs = DefaultLingerDuration)
        {
            // Panel must already be visible.
            if (!singleton.fader.ClassListContains(SlideInClassName))
                return;

            // Transition.
            var transitionCompletion = new TaskCompletionSource<bool>();
            singleton.fader.RegisterCallbackOnce<TransitionEndEvent>(_ => transitionCompletion.TrySetResult(true));
            singleton.fader.AddToClassList(SlideOutClassName);

            await Task.Delay(durationMs);
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

        private void Reset()
        {
            this.fader.style.display = DisplayStyle.None;
            this.fader.RemoveFromClassList(SlideInClassName);
            this.fader.RemoveFromClassList(SlideOutClassName);
        }
    }
}
