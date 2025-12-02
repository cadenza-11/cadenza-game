using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class InteractionIndicator : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        private VisualElement indicator;

        void Start()
        {
            var root = this.uiDocument.rootVisualElement;
            this.indicator = root.Q("indicator");
        }

        public void Show()
        {
            this.indicator.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            this.indicator.style.display = DisplayStyle.None;
        }

        internal void OnPlayerInteractChanged(IInteractable interactable)
        {
            if (interactable != null)
                this.Show();
            else
                this.Hide();
        }
    }
}
