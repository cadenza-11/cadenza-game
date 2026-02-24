using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public abstract class UIPanel : MonoBehaviour
    {
        public enum InputMode
        {
            None,
            Single,
            Multi,
        }

        [SerializeField] public UIDocument uiDocument;
        protected virtual InputMode UIInputMode { get; set; } = InputMode.None;
        protected virtual bool IsWorldSpace { get; set; } = false;
        protected virtual VisualElement InitialFocus { get; } = null;
        protected UIPanel previousPanel;
        protected TemplateContainer root;
        private bool isInitialized;


        private bool isVisible => this.root.style.display == DisplayStyle.Flex;
        public bool IsVisible => this.isVisible;

        void Start()
        {
            if (this.IsWorldSpace && !this.isInitialized)
                this.Initialize();
        }

        public void Initialize()
        {
            if (this.uiDocument == null || this.isInitialized)
                return;

            this.root = (TemplateContainer)this.uiDocument.rootVisualElement;
            this.root.style.display = DisplayStyle.None;
            this.isInitialized = true;
            this.OnInitialize();
        }

        public void Show()
        {
            if (this.isVisible)
                return;

            this.root.style.display = DisplayStyle.Flex;
            this.InitialFocus?.Focus();
            this.OnShow();
        }

        public void Hide()
        {
            if (!this.isVisible)
                return;

            this.OnHide();
            this.root.style.display = DisplayStyle.None;
        }

        public void Toggle()
        {
            if (this.isVisible)
                this.Hide();
            else
                this.Show();
        }

        public void TransitionTo(UIPanel panel, bool hideSelf = true)
        {
            _ = this.TransitionToImplAsync(panel, hideSelf);
        }

        private async Task TransitionToImplAsync(UIPanel panel, bool hideSelf = true)
        {
            await Fader.ShowAsync(setAudio: false);
            if (hideSelf)
            {
                this.Hide();
            }
            if (panel != null)
            {
                panel.previousPanel = this;
                panel.Show();
                Fader.HideImmediate();
            }
        }

        public virtual void OnShow()
        {
        }

        public virtual void OnHide()
        {
        }

        public virtual void OnInitialize()
        {
        }

        public virtual void OnApplicationStop()
        {
        }

        public virtual void OnGameStart()
        {
        }

        public virtual void OnGameStop()
        {
        }

        public virtual void OnUpdate()
        {
        }
    }
}
