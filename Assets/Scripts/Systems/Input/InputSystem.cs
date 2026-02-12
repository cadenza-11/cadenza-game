using System;
using System.Collections.Generic;
using UnityEngine;
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

        private static InputSystem singleton;

        private PlayerInputManager playerInputManager;
        private Dictionary<int, Player> joinedPlayersByID;
        public static IReadOnlyDictionary<int, Player> JoinedPlayersByID => singleton.joinedPlayersByID;
        public static event Action<Player> PlayerJoined;
        public static event Action<Player> PlayerLeft;

        private InputSystemUIInputModule uiInputModule;
        public static event Action<Player> UIPlayerSubmit;
        public static event Action<Player> UIPlayerCancel;
        public static event Action<Vector2, Player> UIPlayerNavigate;

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            this.uiInputModule = this.GetComponent<InputSystemUIInputModule>();

            // Configure player input manager.
            this.joinedPlayersByID = new();
            this.playerInputManager = this.GetComponent<PlayerInputManager>();
            this.playerInputManager.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed;
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

            submitAction.performed += ctx => { if (ctx.performed) UIPlayerSubmit?.Invoke(player); };
            cancelAction.performed += ctx => { if (ctx.performed) UIPlayerCancel?.Invoke(player); };
            navigateAction.performed += ctx => { if (ctx.performed) UIPlayerNavigate?.Invoke(ctx.ReadValue<Vector2>(), player); };
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

                BeatSystem.BeatPlayed += () =>
                {
                    PulseHaptics(
                        haptics,
                        this.beatHapticsMagnitude,
                        this.beatHapticsMagnitude,
                        this.beatHapticsDuration);
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
            singleton.playerInputManager.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenJoinActionIsTriggered;
        }


        /// <summary>
        /// Prevent new devices/players from joining automatically upon button press.
        /// </summary>
        public static void DisableJoining()
        {
            singleton.playerInputManager.joinBehavior = PlayerJoinBehavior.JoinPlayersManually;
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
