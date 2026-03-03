using Cadenza.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.XInput;
using UnityEngine.UIElements;

namespace Cadenza
{
    public partial class CharacterSelect
    {
        private sealed class JoiningPhaseHandler : PhaseHandler
        {
            public JoiningPhaseHandler(CharacterSelect owner)
                : base(owner)
            {
            }

            public override void OnSubmit(Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
                // Add player to the active roster and enter character selection.
                PlayerSystem.AddPlayer(player);
                tracker.Phase = SelectPhase.CharacterSelection;
                tracker.SettingsFocusIndex = 0;
                this.Owner.SetSettingsFocus(playerContainer, tracker);
            }

            public override void OnCancel(Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
                // Allow backing out to the start menu when everyone is unjoined.
                this.Owner.TryQuitToStart();
            }
        }

        private bool TryQuitToStart()
        {
            // Check every player is disconnected.
            foreach (var phase in this.playerTrackers.Values)
            {
                if (phase.Phase != SelectPhase.Joining)
                    return false;
            }

            // Quit.
            this.TransitionTo(this.startMenu);
            return true;
        }

        private void OnPlayerJoined(Player player)
        {
            // Create a tracker for the player.
            if (!this.playerTrackers.ContainsKey(player))
                this.playerTrackers[player] = new PlayerTracker()
                {
                    SettingsFocusIndex = 0,
                    HapticsFocusIndex = 0,
                    SupportsRumbleHaptics = false,
                    HapticsStrength = 1f,
                    HapticsDuration = DefaultHapticsDuration
                };

            // Show initial phase.
            this.playerTrackers[player].Phase = SelectPhase.Joining;
            this.ShowPhase(this.playerContainers[player.ID], SelectPhase.Joining);

            // Enable the new player's UI input.
            InputSystem.SwitchInputMapMultiPlayer(InputSystem.InputMap.UI);

            // Configure input hints for the player's device.
            var device = player.Input.devices[0];
            var controller = device switch
            {
                Keyboard or Mouse => ControllerType.Keyboard,
                XInputController => ControllerType.Xbox,
                DualShockGamepad => ControllerType.PlayStation,
                _ => ControllerType.All,
            };

            var playerContainerElement = this.playerContainers[player.ID].Container;
            playerContainerElement.UpdateInputHintsInHierarchy(controller);

            // Set player name.
            playerContainerElement.Q<Label>("txt_PlayerName").text = player.Name;
            this.UpdateHapticsAvailability(player);

            Debug.Log($"Player {player.ID} joined with controller: {player.Input.devices[0]}");
        }

        private void OnPlayerLeft(Player player)
        {
            // Remove tracker.
            this.playerTrackers.Remove(player);

            // Show disconnected phase.
            this.ShowPhase(this.playerContainers[player.ID], SelectPhase.None);
        }
    }
}
