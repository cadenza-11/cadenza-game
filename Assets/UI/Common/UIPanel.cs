using System;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class UIPanel : ApplicationSystem
    {
        protected CadenzaActions Inputs;
        protected CadenzaActions.UIActions UIInputMap;
        protected TemplateContainer root;

        public override void OnInitialize()
        {
            this.UIInputMap = InputSystem.UIInputMap;
        }
        public virtual void Show()
        {
            this.UIInputMap.Enable();
            this.root.style.display = DisplayStyle.Flex;
        }

        public virtual void Hide()
        {
            this.UIInputMap.Disable();
            this.root.style.display = DisplayStyle.None;
        }
    }
}
