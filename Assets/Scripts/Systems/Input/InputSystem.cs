using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cadenza
{
    public class InputSystem : ApplicationSystem,
        CadenzaActions.IUIActions
    {
        private static InputSystem singleton;

        private CadenzaActions inputActions;
        private CadenzaActions.UIActions uiInputMap;
        public static CadenzaActions.UIActions UIInputMap => singleton.uiInputMap;

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            this.inputActions = new CadenzaActions();

            this.uiInputMap = this.inputActions.UI;
            this.uiInputMap.AddCallbacks(this);

            this.uiInputMap.Enable();
        }

        public override void OnApplicationStop()
        {
            this.uiInputMap.Disable();
        }

        // This will be called by Unity.
        private void OnPlayerJoined(PlayerInput player)
        {

        }

        // This will be called by Unity.
        private void OnPlayerLeft(PlayerInput player)
        {

        }

        #region Player Interface Methods

        #endregion
        #region UI Interface Methods

        public void OnNavigate(InputAction.CallbackContext context)
        {
        }

        public void OnSubmit(InputAction.CallbackContext context)
        {
        }

        public void OnCancel(InputAction.CallbackContext context)
        {
        }

        public void OnPoint(InputAction.CallbackContext context)
        {
        }

        public void OnClick(InputAction.CallbackContext context)
        {
        }

        public void OnRightClick(InputAction.CallbackContext context)
        {
        }

        public void OnMiddleClick(InputAction.CallbackContext context)
        {
        }

        public void OnScrollWheel(InputAction.CallbackContext context)
        {
        }

        public void OnToggleDebug(InputAction.CallbackContext context)
        {
        }

        #endregion
    }
}
