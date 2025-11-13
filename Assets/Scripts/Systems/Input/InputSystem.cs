using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace Cadenza
{
    /// <summary>
    /// Handles enabling and disabling of input actions, input action maps, and player input.
    /// </summary>
    public class InputSystem : ApplicationSystem, CadenzaActions.IUIActions
    {
        private static InputSystem singleton;

        private CadenzaActions inputActions;
        private CadenzaActions.UIActions uiInputMap;
        public static CadenzaActions.UIActions UIInputMap => singleton.uiInputMap;

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            // Configure input maps.
            this.inputActions = new CadenzaActions();

            this.uiInputMap = this.inputActions.UI;
            this.uiInputMap.AddCallbacks(this);
            this.uiInputMap.Enable();
        }

        public override void OnApplicationStop()
        {
            this.uiInputMap.Disable();
        }

        #region Public Static Methods

        public static Player GetPlayerFromDevice(InputDevice device)
        {
            var user = InputUser.FindUserPairedToDevice(device);
            foreach (var player in PlayerSystem.PlayersByID.Values)
            {
                if (player.Input.user == user)
                    return player;
            }
            return null;
        }

        /// <summary>
        /// Disables all players' input except for a single player.
        /// </summary>
        public static void EnableSinglePlayerInput(Player player)
        {
            EnableInputActionMapForPlayers("Player", disableOthers: true, player);
            EnableInputActionMapForPlayers("UI", disableOthers: true, player);
        }

        public static void EnableInputActionMapForPlayers(string mapName, bool disableOthers, params Player[] players)
        {
            if (disableOthers)
            {
                foreach (var player in PlayerSystem.PlayersByID.Values)
                    player.Input.actions.FindActionMap(mapName)?.Disable();
            }

            foreach (var player in players)
                player.Input.actions.FindActionMap(mapName)?.Enable();
        }

        public static void DisableInputActionMapForPlayers(string mapName, bool enableOthers, params Player[] players)
        {
            if (enableOthers)
            {
                foreach (var player in PlayerSystem.PlayersByID.Values)
                    player.Input.actions.FindActionMap(mapName)?.Enable();
            }

            foreach (var player in players)
                player.Input.actions.FindActionMap(mapName)?.Disable();
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

        public void OnJoin(InputAction.CallbackContext context)
        {
        }

        #endregion
    }
}
