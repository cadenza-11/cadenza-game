using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Haptics;
using UnityEngine.UIElements;

namespace Cadenza
{
    public partial class CharacterSelect
    {
        private const int LastHapticsIndex = 2;
        private const float HapticsStrengthStep = 0.1f;
        private const float HapticsDurationStep = 0.05f;
        private const float DefaultHapticsDuration = 0.10f;

        private sealed class HapticsPhaseHandler : PhaseHandler
        {
            public HapticsPhaseHandler(CharacterSelect owner)
                : base(owner)
            {
            }

            public override void OnSubmit(Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
                // Submit only exits when "Back" is focused.
                if (tracker.HapticsFocusIndex == LastHapticsIndex)
                    tracker.Phase = SelectPhase.Settings;
            }

            public override void OnCancel(Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
                // Cancel always returns to settings.
                tracker.Phase = SelectPhase.Settings;
            }

            public override void OnNavigate(MoveDirection moveDirection, Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
                // Move focus vertically, adjust selected slider horizontally.
                if (moveDirection == MoveDirection.Up)
                    tracker.HapticsFocusIndex = Mathf.Max(0, tracker.HapticsFocusIndex - 1);
                else if (moveDirection == MoveDirection.Down)
                    tracker.HapticsFocusIndex = Mathf.Min(LastHapticsIndex, tracker.HapticsFocusIndex + 1);
                else if (moveDirection == MoveDirection.Left && tracker.HapticsFocusIndex == 0)
                    this.Owner.SetHapticsStrength(player, tracker.HapticsStrength - HapticsStrengthStep);
                else if (moveDirection == MoveDirection.Right && tracker.HapticsFocusIndex == 0)
                    this.Owner.SetHapticsStrength(player, tracker.HapticsStrength + HapticsStrengthStep);
                else if (moveDirection == MoveDirection.Left && tracker.HapticsFocusIndex == 1)
                    this.Owner.SetHapticsDuration(player, tracker.HapticsDuration - HapticsDurationStep);
                else if (moveDirection == MoveDirection.Right && tracker.HapticsFocusIndex == 1)
                    this.Owner.SetHapticsDuration(player, tracker.HapticsDuration + HapticsDurationStep);

                this.Owner.SetHapticsFocus(playerContainer, tracker.HapticsFocusIndex);
            }
        }

        private void OnPressHapticsBack(PlayerContainer playerContainer)
        {
            // Ignore button presses when not currently editing haptics.
            if (!this.TryGetPlayerByContainer(playerContainer, out _, out PlayerTracker tracker) ||
                tracker.Phase != SelectPhase.Haptics)
                return;

            // Focus "Back" and return to settings.
            tracker.HapticsFocusIndex = LastHapticsIndex;
            this.SetHapticsFocus(playerContainer, tracker.HapticsFocusIndex);
            tracker.Phase = SelectPhase.Settings;
            this.ShowPhase(playerContainer, tracker.Phase);
        }

        private void OnHapticsStrengthSliderChanged(PlayerContainer playerContainer, float value)
        {
            // Update the displayed value immediately.
            playerContainer.HapticsStrengthValue.text = this.GetHapticsStrengthDisplayText(value);

            if (!this.TryGetPlayerByContainer(playerContainer, out Player player, out PlayerTracker tracker))
                return;

            // Persist and apply updated haptics strength.
            tracker.HapticsStrength = value;
            InputSystem.SetHapticsStrengthForPlayer(player, value);
        }

        private void OnHapticsDurationSliderChanged(PlayerContainer playerContainer, float value)
        {
            // Update the displayed value immediately.
            playerContainer.HapticsDurationValue.text = this.GetHapticsDurationDisplayText(value);

            if (!this.TryGetPlayerByContainer(playerContainer, out Player player, out PlayerTracker tracker))
                return;

            // Persist and apply updated haptics duration.
            tracker.HapticsDuration = value;
            InputSystem.SetHapticsDurationForPlayer(player, value);
        }

        private void SetHapticsStrength(Player player, float value)
        {
            if (!this.playerTrackers.TryGetValue(player, out PlayerTracker tracker))
                return;

            // Clamp and apply strength to UI and input system.
            tracker.HapticsStrength = Mathf.Clamp01(value);

            var container = this.playerContainers[player.ID];
            container.HapticsStrengthSlider.SetValueWithoutNotify(tracker.HapticsStrength);
            container.HapticsStrengthValue.text = this.GetHapticsStrengthDisplayText(tracker.HapticsStrength);
            InputSystem.SetHapticsStrengthForPlayer(player, tracker.HapticsStrength);
        }

        private void SetHapticsDuration(Player player, float value)
        {
            if (!this.playerTrackers.TryGetValue(player, out PlayerTracker tracker))
                return;

            // Clamp and apply duration to UI and input system.
            tracker.HapticsDuration = Mathf.Clamp(value, 0f, 1f);

            var container = this.playerContainers[player.ID];
            container.HapticsDurationSlider.SetValueWithoutNotify(tracker.HapticsDuration);
            container.HapticsDurationValue.text = this.GetHapticsDurationDisplayText(tracker.HapticsDuration);
            InputSystem.SetHapticsDurationForPlayer(player, tracker.HapticsDuration);
        }

        private void SetHapticsFocus(PlayerContainer playerContainer, int focusedIndex)
        {
            // Highlight whichever haptics row currently has focus.
            for (int i = 0; i < playerContainer.HapticsItems.Length; i++)
            {
                if (i == focusedIndex)
                    playerContainer.HapticsItems[i].AddToClassList("is_focus");
                else
                    playerContainer.HapticsItems[i].RemoveFromClassList("is_focus");
            }
        }

        private string GetHapticsStrengthDisplayText(float value)
        {
            return $"{Mathf.RoundToInt(value * 100f)}%";
        }

        private string GetHapticsDurationDisplayText(float value)
        {
            return $"{Mathf.RoundToInt(value * 1000f)}ms";
        }

        private void UpdateHapticsAvailability(Player player)
        {
            if (!this.playerTrackers.TryGetValue(player, out PlayerTracker tracker))
                return;

            // Detect rumble support and show/hide the haptics settings entry.
            var container = this.playerContainers[player.ID];
            tracker.SupportsRumbleHaptics = this.SupportsRumbleHaptics(player);
            container.HapticsButton.style.display =
                tracker.SupportsRumbleHaptics
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            // Force out of haptics phase if support is lost.
            if (!tracker.SupportsRumbleHaptics && tracker.Phase == SelectPhase.Haptics)
                tracker.Phase = SelectPhase.Settings;

            // Keep settings focus aligned to the active settings layout.
            this.ClampSettingsFocus(container, tracker);
        }

        private bool SupportsRumbleHaptics(Player player)
        {
            // Any connected dual-motor device qualifies.
            foreach (var device in player.Input.devices)
            {
                if (device is IDualMotorRumble)
                    return true;
            }

            return false;
        }
    }
}
