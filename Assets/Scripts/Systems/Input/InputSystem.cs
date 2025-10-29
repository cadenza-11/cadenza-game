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

        #region Player Interface Methods

        public void OnMove(InputAction.CallbackContext context)
        {
            int id = context.control.device.deviceId;
            var input = context.performed ? context.ReadValue<Vector2>() : Vector2.zero;

            if (PlayerSystem.TryGetPlayerByID(id, out Player player))
                player.Character.Move(input);
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
        }

        public void OnAttackLight(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            int id = context.control.device.deviceId;
            if (PlayerSystem.TryGetPlayerByID(id, out Player player))
                player.Character.WeakAttack();
        }

        public void OnAttackHeavy(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            int id = context.control.device.deviceId;
            if (PlayerSystem.TryGetPlayerByID(id, out Player player))
                player.Character.StrongAttack();
        }

        public void OnAttackSpecial(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            int id = context.control.device.deviceId;
            if (PlayerSystem.TryGetPlayerByID(id, out Player player))
                player.Character.SpecialAttack();
        }

        public void OnAttackTeam(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            int id = context.control.device.deviceId;
            if (PlayerSystem.TryGetPlayerByID(id, out Player player))
                player.Character.StartTeamAttk();
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
            DebugConsole.ToggleVisibility();
        }

        #endregion
    }
}
