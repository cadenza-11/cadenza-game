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

            this.uiInputMap = this.inputActions.UI;
            this.uiInputMap.AddCallbacks(this);

            this.playerInputMap = this.inputActions.Player;
            this.playerInputMap.AddCallbacks(this);

            this.uiInputMap.Enable();
            this.playerInputMap.Disable();
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
            var player = PlayerSystem.GetPlayerByID(deviceID).Input;
            if (player != null)
            {
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
            
        }

        public static void RestorePlayerInputActionToDefault(int deviceID, string actionName)
        {
            var player = PlayerSystem.GetPlayerByID(deviceID).Input;
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
                PlayerSystem.OnAttackLight(context.control.device.deviceId);
        }

        public void OnAttackHeavy(InputAction.CallbackContext context)
        {
            if (context.performed)
                PlayerSystem.OnAttackHeavy(context.control.device.deviceId);
        }

        public void OnAttackSpecial(InputAction.CallbackContext context)
        {
            if (context.performed)
                PlayerSystem.OnAttackSpecial(context.control.device.deviceId);
        }

        public void OnAttackTeam(InputAction.CallbackContext context)
        {
            if (context.performed)
                PlayerSystem.OnAttackTeam(context.control.device.deviceId);
        }

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
