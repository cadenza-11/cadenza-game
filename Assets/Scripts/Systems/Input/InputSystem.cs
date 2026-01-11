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
        public static InputSystemUIInputModule UIInputModule => singleton.uiInputModule;

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            this.uiInputModule = this.GetComponent<InputSystemUIInputModule>();
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
    }
}
