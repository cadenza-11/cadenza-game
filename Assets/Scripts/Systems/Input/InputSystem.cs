using System;
using System.Collections.Generic;
using Cadenza.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Haptics;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Users;

namespace Cadenza
{
    /// <summary>
    /// Handles enabling and disabling of input actions, input action maps, and player input.
    /// </summary>
    [RequireComponent(typeof(InputSystemUIInputModule), typeof(PlayerInputManager))]
    public class InputSystem : ApplicationSystem
    {
        public enum InputMap
        {
            Player,
            UI,
        }

        [SerializeField]
        private float beatHapticsDuration;

        [SerializeField, Range(0, 1)]
        private float beatHapticsMagnitude;

        private bool hapticsEnabled = true;

        private static readonly Dictionary<InputMap, string> mapNames = new()
        {
            { InputMap.Player, "Player" },
            { InputMap.UI, "UI" },
        };

        private readonly Dictionary<Player, MoveDirection> previousNavigationDirections = new();
        private readonly Dictionary<Player, float> controllerHapticsStrength = new();
        private readonly Dictionary<Player, float> controllerHapticsDuration = new();

        private static InputSystem singleton;

        private PlayerInputManager playerInputManager;
        private Dictionary<int, Player> joinedPlayersByID;
        public static IReadOnlyDictionary<int, Player> JoinedPlayersByID => singleton.joinedPlayersByID;
        public static event Action<Player> PlayerJoined;
        public static event Action<Player> PlayerLeft;

        private InputSystemUIInputModule uiInputModule;
        public static event Action<Player> UIPlayerSubmit;
        public static event Action<Player> UIPlayerCancel;
        public static event Action<MoveDirection, Player> UIPlayerNavigate;

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            this.uiInputModule = this.GetComponent<InputSystemUIInputModule>();

            // The Input System UI Input Module will enable any map used in any of its
            // bindings ("UI" map). This conflicts with the PlayerInput component, which
            // disables all maps except the one configured to be the default map.

            // Unless the PlayerInput default map is set to UI, disable the UI map here
            // to prevent UI actions from triggering before initialization.
            this.uiInputModule.actionsAsset.Disable();

            // Configure player input manager.
            this.joinedPlayersByID = new();
            this.playerInputManager = this.GetComponent<PlayerInputManager>();
            this.playerInputManager.onPlayerJoined += this.OnPlayerJoined;
            this.playerInputManager.onPlayerLeft += this.OnPlayerLeft;
        }

        public override void OnApplicationStop()
        {
            this.UnregisterAllHaptics();
        }

        private void OnPlayerJoined(PlayerInput playerInput)
        {
            playerInput.transform.SetParent(this.transform);
            var player = playerInput.GetComponent<Player>();

            // Configure ID.
            int id = playerInput.playerIndex;
            player.Initialize(id, playerInput);
            this.joinedPlayersByID[id] = player;

            this.RegisterPlayerNavigationEvents(player);
            this.RegisterPlayerDebugEvents(player);
            this.RegisterPlayerBeatHaptics(player);

            playerInput.onDeviceLost += input => PlayerLeft?.Invoke(player);
            playerInput.onDeviceRegained += input => PlayerJoined?.Invoke(player);

            Debug.Log($"Player joined using device scheme {playerInput.currentControlScheme}. (id={id})");
            PlayerJoined?.Invoke(player);
        }

        private void OnPlayerLeft(PlayerInput playerInput)
        {
            if (this.joinedPlayersByID.TryGetValue(playerInput.playerIndex, out Player player))
            {
                this.joinedPlayersByID.Remove(playerInput.playerIndex);
                PlayerLeft?.Invoke(player);
            }
        }

        private void RegisterPlayerNavigationEvents(Player player)
        {
            // Register for per-player UI actions.
            var submitAction = player.Input.actions.FindAction("Submit", throwIfNotFound: true);
            var cancelAction = player.Input.actions.FindAction("Cancel", throwIfNotFound: true);
            var navigateAction = player.Input.actions.FindAction("Navigate", throwIfNotFound: true);
            this.previousNavigationDirections[player] = MoveDirection.None;

            submitAction.performed += ctx => { if (ctx.performed) UIPlayerSubmit?.Invoke(player); };
            cancelAction.performed += ctx => { if (ctx.performed) UIPlayerCancel?.Invoke(player); };
            navigateAction.performed += ctx =>
            {
                if (!ctx.performed)
                    return;

                // Determine move direction.
                // Avoid sending navigation events for the same direction consecutively.
                var previousDirection = this.previousNavigationDirections[player];
                var nextDirection = UI.GetMoveDirection(ctx.ReadValue<Vector2>());
                this.previousNavigationDirections[player] = nextDirection;

                if (nextDirection != MoveDirection.None &&
                    nextDirection != previousDirection)
                    UIPlayerNavigate?.Invoke(nextDirection, player);
            };
        }

        private void RegisterPlayerDebugEvents(Player player)
        {
            // Keep the debug map always active.
            var map = player.Input.actions.FindActionMap("Debug", throwIfNotFound: true);
            map.Enable();

            var toggleDebugAction = map.FindAction("ToggleVisibility", throwIfNotFound: true);
            toggleDebugAction.performed += ctx => { if (ctx.performed) UISystem.FindPanel<DebugConsole>().Toggle(); };
        }

        private void RegisterPlayerBeatHaptics(Player player)
        {
            // Register haptics.
            foreach (var device in player.Input.devices)
            {
                if (device is not IDualMotorRumble haptics)
                    continue;

                if (!this.controllerHapticsStrength.ContainsKey(player))
                    this.controllerHapticsStrength[player] = this.beatHapticsMagnitude;

                if (!this.controllerHapticsDuration.ContainsKey(player))
                    this.controllerHapticsDuration[player] = this.beatHapticsDuration;

                BeatSystem.BeatPlayed += () =>
                {
                    PulseHaptics(
                        haptics,
                        this.controllerHapticsStrength[player],
                        this.controllerHapticsStrength[player],
                        this.controllerHapticsDuration[player]);
                };
            }
        }

        private void UnregisterAllHaptics()
        {
            foreach (var device in Gamepad.all)
                device.ResetHaptics();
        }

        #region Public Static Methods

        public static bool IsPlayerJoined(int id)
        {
            return singleton.joinedPlayersByID.TryGetValue(id, out _);
        }

        /// <summary>
        /// Allow new devices/players to join when pressing a button.
        /// </summary>
        public static void EnableJoining()
        {
            singleton.playerInputManager.EnableJoining();
        }


        /// <summary>
        /// Prevent new devices/players from joining automatically upon button press.
        /// </summary>
        public static void DisableJoining()
        {
            singleton.playerInputManager.DisableJoining();
        }

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

            foreach (var player in JoinedPlayersByID.Values)
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

            foreach (var player in JoinedPlayersByID.Values)
            {
                player.Input.DeactivateInput();
                player.Input.SwitchCurrentActionMap(mapName);
                player.Input.ActivateInput();
            }
        }

        public static Player GetPlayerFromDevice(InputDevice device)
        {
            var user = InputUser.FindUserPairedToDevice(device);
            foreach (var player in JoinedPlayersByID.Values)
            {
                if (player.Input.user == user)
                    return player;
            }
            return null;
        }

        /// <summary>
        /// Globally enable or disable controller rumble haptics.
        /// </summary>
        public static void SetHapticsEnabled(bool enabled)
        {
            singleton.hapticsEnabled = enabled;
        }

        /// <summary>
        /// Sets the strength of the haptics on each of a
        /// player's connected devices that use haptics.
        /// </summary>
        public static void SetHapticsStrengthForPlayer(Player player, float value)
        {
            singleton.controllerHapticsStrength[player] = Mathf.Clamp(value, 0, 1);
        }

        /// <summary>
        /// Sets the duration of the haptics on each of a
        /// player's connected devices that use haptics.
        /// </summary>
        public static void SetHapticsDurationForPlayer(Player player, float value)
        {
            singleton.controllerHapticsDuration[player] = Mathf.Clamp(value, 0, (float)BeatSystem.SecondsPerBeat);
        }

        /// <summary>
        /// Enables rumble haptics on a device for a set period of time.
        /// Device must be a Gamepad or implement the IDualMotorRumble interface.
        /// </summary>
        /// <param name="haptics">The device to enable haptics on</param>
        /// <param name="lowFrequency">The requested speed of the left motor on the controller, from 0-1</param>
        /// <param name="highFrequency">The requested speed of the right motor on the controller, from 0-1</param>
        /// <param name="duration">The amount of time before the motors are turned off</param>
        public static void PulseHaptics(IDualMotorRumble haptics, float lowFrequency, float highFrequency, float duration)
        {
            if (!singleton.hapticsEnabled)
                return;

            haptics.SetMotorSpeeds(lowFrequency, highFrequency);
            haptics.ResumeHaptics();
            Timer.ScheduleAsync(duration, () => haptics.SetMotorSpeeds(0, 0));
        }

        #endregion
    }
}
