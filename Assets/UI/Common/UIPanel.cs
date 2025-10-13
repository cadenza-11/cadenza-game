using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Cadenza
{
    public abstract class UIPanel : ApplicationSystem
    {
        protected CadenzaActions Inputs;
        protected InputActionMap UIActions;
        protected TemplateContainer root; 
        public override void OnStart()
        {
            UIActions = Inputs.UI;
        }
        public virtual void Show()
        {
            UIActions.Enable();
            this.root.style.display = DisplayStyle.Flex;
        }

        public virtual void Hide()
        {
            UIActions.Disable();
            this.root.style.display = DisplayStyle.None;
        }
    }
}
