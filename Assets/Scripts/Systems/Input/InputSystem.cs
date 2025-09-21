using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cadenza
{
    public class InputSystem : ApplicationSystem,
        CadenzaActions.IPlayerActions,
        CadenzaActions.IUIActions
    {
        private static InputSystem singleton;

        private CadenzaActions inputActions;
        private CadenzaActions.PlayerActions playerInputMap;
        private CadenzaActions.UIActions uiInputMap;

        public static CadenzaActions.PlayerActions PlayerInputMap => singleton.playerInputMap;
        public static CadenzaActions.UIActions UIInputMap => singleton.uiInputMap;

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            this.inputActions = new CadenzaActions();

            this.uiInputMap = inputActions.UI;
            // this.uiInputMap.AddCallbacks(this);

            this.playerInputMap = inputActions.Player;
            // this.playerInputMap.AddCallbacks(this);

            this.uiInputMap.Enable();
        }

        public override void OnApplicationStop()
        {
            this.uiInputMap.Disable();
        }

        public override void OnGameStart()
        {
            this.playerInputMap.Enable();
        }

        public override void OnGameStop()
        {
            this.playerInputMap.Disable();
        }

        #region Player Interface Methods

        public void OnMove(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        #endregion
        #region UI Interface Methods

        public void OnNavigate(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnSubmit(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnCancel(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnPoint(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnClick(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnRightClick(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnMiddleClick(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnScrollWheel(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnTrackedDevicePosition(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnTrackedDeviceOrientation(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnToggleDebug(InputAction.CallbackContext context)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
