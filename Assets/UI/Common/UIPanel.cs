using System;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Cadenza
{
    public abstract class UIPanel : ApplicationSystem
    {
        protected CadenzaActions Inputs;
        protected InputActionMap UIActions;
        protected TemplateContainer root;
        public override void OnInitialize()
        {
            this.UIActions = InputSystem.UIInputMap;
            UnityEngine.Debug.Log(this.UIActions == null);
        }
        public virtual void Show()
        {
            this.UIActions.Enable();
            this.root.style.display = DisplayStyle.Flex;
        }

        public virtual void Hide()
        {
            this.UIActions.Disable();
            this.root.style.display = DisplayStyle.None;
        }
    }
}
