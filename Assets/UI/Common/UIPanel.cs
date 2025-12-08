using UnityEngine.UIElements;

namespace Cadenza
{
    public abstract class UIPanel : ApplicationSystem
    {
        protected CadenzaActions Inputs;
        protected TemplateContainer root;
        public UIPanel previousPanel;

        public virtual void Show()
        {
            // InputSystem.UIInputMap.Enable();
        }

        public virtual void Hide()
        {
            // InputSystem.UIInputMap.Disable();
        }
    }
}
