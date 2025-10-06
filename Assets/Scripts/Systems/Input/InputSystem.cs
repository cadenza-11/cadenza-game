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
            this.playerInputMap.AddCallbacks(this);

            // this.uiInputMap.Enable();
            this.playerInputMap.Enable();
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

        public static void RebindPlayerInputAction(int deviceID, string actionName)
        {
            var player = PlayerSystem.GetPlayerByID(deviceID);
            var playerAction = player.actions.FindAction(actionName, throwIfNotFound: true);
            var rebindOp = playerAction.PerformInteractiveRebinding()
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithControlsExcluding("<Gamepad>/Start")
                .WithControlsExcluding("<Keyboard>/escape")
                .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => { });

            rebindOp.Start();
        }

        public static void RestorePlayerInputActionToDefault(int deviceID, string actionName)
        {
            var player = PlayerSystem.GetPlayerByID(deviceID);
            var playerAction = player.actions.FindAction(actionName, throwIfNotFound: true);
            InputActionRebindingExtensions.RemoveAllBindingOverrides(playerAction);
        }

        #region Player Interface Methods

        public void OnMove(InputAction.CallbackContext context)
        {
            var input = context.performed ? context.ReadValue<Vector2>() : Vector2.zero;
            PlayerSystem.OnMove(context.control.device.deviceId, input);
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (context.performed)
                PlayerSystem.OnInteract(context.control.device.deviceId);
        }

        public void OnAttackLight(InputAction.CallbackContext context)
        {
            if (context.performed)
                PlayerSystem.OnInteract(context.control.device.deviceId);
        }

        public void OnAttackHeavy(InputAction.CallbackContext context)
        {
            if (context.performed)
                PlayerSystem.OnInteract(context.control.device.deviceId);
        }

        public void OnAttackSpecial(InputAction.CallbackContext context)
        {
            if (context.performed)
                PlayerSystem.OnInteract(context.control.device.deviceId);
        }

        public void OnAttackTeam(InputAction.CallbackContext context)
        {
            if (context.performed)
                PlayerSystem.OnInteract(context.control.device.deviceId);
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

        public void OnToggleDebug(InputAction.CallbackContext context)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
