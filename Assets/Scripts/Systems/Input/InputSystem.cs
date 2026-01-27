using System;
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
    public class InputSystem : ApplicationSystem
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

        private InputSystemUIInputModule uiInputModule;
        public static event Action<Player> UIPlayerSubmit;
        public static event Action<Player> UIPlayerCancel;
        public static event Action<Vector2, Player> UIPlayerNavigate;

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            this.uiInputModule = this.GetComponent<InputSystemUIInputModule>();
            PlayerSystem.PlayerJoined += this.OnPlayerJoined;
        }

        private void OnPlayerJoined(Player player)
        {
            this.RegisterPlayerNavigationEvents(player);
            this.RegisterPlayerDebugEvents(player);
        }

        private void RegisterPlayerNavigationEvents(Player player)
        {
            // Register for per-player UI actions.
            var submitAction = player.Input.actions.FindAction("Submit");
            var cancelAction = player.Input.actions.FindAction("Cancel");
            var navigateAction = player.Input.actions.FindAction("Navigate");

            submitAction.performed += ctx => { if (ctx.performed) UIPlayerSubmit?.Invoke(player); };
            cancelAction.performed += ctx => { if (ctx.performed) UIPlayerCancel?.Invoke(player); };
            navigateAction.performed += ctx => { if (ctx.performed) UIPlayerNavigate?.Invoke(ctx.ReadValue<Vector2>(), player); };
        }

        private void RegisterPlayerDebugEvents(Player player)
        {
            var toggleDebugAction = player.Input.actions.FindAction("Toggle/Debug");
            toggleDebugAction.performed += ctx => { if (ctx.performed) UISystem.FindPanel<DebugConsole>().Toggle(); };
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
            if (!mapNames.TryGetValue(inputMap, out string mapName))
                return;

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
            if (!mapNames.TryGetValue(inputMap, out string mapName))
                return;

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
    }
}
