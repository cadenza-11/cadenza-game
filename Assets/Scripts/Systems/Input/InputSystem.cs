using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Users;

namespace Cadenza
{
    /// <summary>
    /// Handles enabling and disabling of input actions, input action maps, and player input.
    /// </summary>
    [RequireComponent(typeof(InputSystemUIInputModule))]
    public class InputSystem : ApplicationSystem, CadenzaActions.IUIActions
    {
        public enum InputMap
        {
            Player,
            UI,
        }

        private static readonly Dictionary<InputMap, string> mapNames = new()
        {
            { InputMap.Player, "Player" },
            { InputMap.UI, "UI" },
        };

        private static InputSystem singleton;

        private CadenzaActions inputActions;
        private CadenzaActions.UIActions uiInputMap;
        private InputSystemUIInputModule uiInputModule;
        public static CadenzaActions.UIActions UIInputMap => singleton.uiInputMap;
        public static InputSystemUIInputModule UIInputModule => singleton.uiInputModule;

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            // Configure input maps.
            this.inputActions = new CadenzaActions();

            this.uiInputMap = this.inputActions.UI;
            this.uiInputMap.AddCallbacks(this);
            this.uiInputMap.Enable();

            this.uiInputModule = this.GetComponent<InputSystemUIInputModule>();
        }

        public override void OnApplicationStop()
        {
            this.uiInputMap.Disable();
        }

        #region Public Static Methods

        /// <summary>
        /// Enables the requested input map on the host player and disables all
        /// other input maps. Disables all input for players other than the host player.
        /// </summary>
        /// <param name="inputMap">The input map to enable</param>
        /// <param name="hostPlayer">The player to give sole input</param>
        public static void SwitchInputMapSinglePlayer(InputMap inputMap, Player hostPlayer)
        {
            Debug.Assert(mapNames.TryGetValue(inputMap, out string mapName));

            foreach (var player in PlayerSystem.PlayersByID.Values)
                player.Input.DeactivateInput();

            hostPlayer.Input.SwitchCurrentActionMap(mapName);
            hostPlayer.Input.ActivateInput();
        }

        /// <summary>
        /// Enables the requested input map for all players
        /// and disables all other input maps.
        /// </summary>
        /// <param name="inputMap">The input map to enable</param>
        public static void SwitchInputMapMultiPlayer(InputMap inputMap)
        {
            Debug.Assert(mapNames.TryGetValue(inputMap, out string mapName));

            foreach (var player in PlayerSystem.PlayersByID.Values)
            {
                player.Input.DeactivateInput();
                player.Input.SwitchCurrentActionMap(mapName);
                player.Input.ActivateInput();
            }
        }

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

        public void OnUnpause(InputAction.CallbackContext context)
        {
            if (context.performed)
                ApplicationController.UnpauseGame();
        }

        #endregion
    }
}
