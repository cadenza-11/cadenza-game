using UnityEngine.UIElements;

namespace Cadenza
{
    public class InteractionIndicator : UIPanel
    {
        protected override bool IsWorldSpace => true;
        private InputHint indicator;

        public override void OnInitialize()
        {
            this.indicator = this.root.Q<InputHint>("indicator");
        }

        public void SetInputHint(ControllerType controller)
        {
            UnityEngine.Debug.Log($"Setting interaction button to {controller}");
            this.indicator.ShowForControllerType(controller);
        }

        internal void OnPlayerInteractChanged(IInteractable interactable)
        {
            if (interactable != null)
                this.Show();
            else
                this.Hide();
        }

        public override void OnShow()
        {
            this.indicator.style.display = DisplayStyle.Flex;
        }

        public override void OnHide()
        {
            this.indicator.style.display = DisplayStyle.None;
        }
    }
}
