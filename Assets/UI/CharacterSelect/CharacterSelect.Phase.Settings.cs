using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Cadenza
{
    public partial class CharacterSelect
    {
        private sealed class SettingsPhaseHandler : PhaseHandler
        {
            public SettingsPhaseHandler(CharacterSelect owner)
                : base(owner)
            {
            }

            public override void OnSubmit(Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
                // Ensure the focused settings item is valid.
                this.Owner.ClampSettingsFocus(playerContainer, tracker);
                var focusedSetting = this.Owner.GetSettingsItems(playerContainer, tracker)[tracker.SettingsFocusIndex];

                // Activate the currently focused settings action.
                if (focusedSetting == playerContainer.RecalibrateButton)
                {
                    // Enter calibration flow.
                    this.Owner.BeginCalibration(player);
                }
                else if (focusedSetting == playerContainer.RenameButton)
                {
                    // Enter player naming flow.
                    this.Owner.BeginRenaming(player);
                }
                else if (focusedSetting == playerContainer.HapticsButton)
                {
                    // Open haptics tuning flow.
                    tracker.Phase = SelectPhase.Haptics;
                    tracker.HapticsFocusIndex = 0;
                    this.Owner.SetHapticsFocus(playerContainer, tracker.HapticsFocusIndex);
                }
                else
                {
                    // Continue to ready check.
                    tracker.Phase = SelectPhase.Ready;
                }
            }

            public override void OnCancel(Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
                // Return to character selection and free any chosen class.
                this.Owner.DeselectCharacter(player);
                tracker.Phase = SelectPhase.CharacterSelection;
            }

            public override void OnNavigate(MoveDirection moveDirection, Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
                int lastSettingsIndex = this.Owner.GetSettingsItems(playerContainer, tracker).Length - 1;

                // Move focus up or down through visible settings items.
                if (moveDirection == MoveDirection.Up)
                    tracker.SettingsFocusIndex = Mathf.Max(0, tracker.SettingsFocusIndex - 1);
                else if (moveDirection == MoveDirection.Down)
                    tracker.SettingsFocusIndex = Mathf.Min(lastSettingsIndex, tracker.SettingsFocusIndex + 1);

                this.Owner.SetSettingsFocus(playerContainer, tracker);
            }
        }

        private void OnPressRecalibrate(PlayerContainer playerContainer)
        {
            // Ignore clicks when this player is not actively in settings.
            if (!this.TryGetPlayerByContainer(playerContainer, out Player player, out PlayerTracker tracker) ||
                tracker.Phase != SelectPhase.Settings)
                return;

            // Mirror controller focus and enter calibration flow.
            tracker.SettingsFocusIndex = 0;
            this.SetSettingsFocus(playerContainer, tracker);
            this.BeginCalibration(player);
            this.ShowPhase(playerContainer, tracker.Phase);
        }

        private void OnPressRename(PlayerContainer playerContainer)
        {
            // Ignore clicks when this player is not actively in settings.
            if (!this.TryGetPlayerByContainer(playerContainer, out Player player, out PlayerTracker tracker) ||
                tracker.Phase != SelectPhase.Settings)
                return;

            // Mirror controller focus and enter naming flow.
            tracker.SettingsFocusIndex = 1;
            this.SetSettingsFocus(playerContainer, tracker);
            this.BeginRenaming(player);
            this.ShowPhase(playerContainer, tracker.Phase);
        }

        private void OnPressHaptics(PlayerContainer playerContainer)
        {
            // Ignore clicks when haptics flow is not available.
            if (!this.TryGetPlayerByContainer(playerContainer, out _, out PlayerTracker tracker) ||
                tracker.Phase != SelectPhase.Settings ||
                !tracker.SupportsRumbleHaptics)
                return;

            // Mirror controller focus and enter haptics flow.
            tracker.SettingsFocusIndex = 2;
            tracker.HapticsFocusIndex = 0;
            this.SetSettingsFocus(playerContainer, tracker);
            this.SetHapticsFocus(playerContainer, tracker.HapticsFocusIndex);
            tracker.Phase = SelectPhase.Haptics;
            this.ShowPhase(playerContainer, tracker.Phase);
        }

        private void OnPressNext(PlayerContainer playerContainer)
        {
            // Ignore clicks when this player is not actively in settings.
            if (!this.TryGetPlayerByContainer(playerContainer, out _, out PlayerTracker tracker) ||
                tracker.Phase != SelectPhase.Settings)
                return;

            // Focus "Next" and continue to ready.
            tracker.SettingsFocusIndex = this.GetSettingsItems(playerContainer, tracker).Length - 1;
            this.SetSettingsFocus(playerContainer, tracker);
            tracker.Phase = SelectPhase.Ready;
            this.ShowPhase(playerContainer, tracker.Phase);
        }

        private void SetSettingsFocus(PlayerContainer playerContainer, PlayerTracker tracker)
        {
            // Ensure focus index is valid for the current settings layout.
            this.ClampSettingsFocus(playerContainer, tracker);

            // Clear prior focus state.
            foreach (var item in playerContainer.SettingsItemsWithHaptics)
                item.RemoveFromClassList("is_focus");

            // Highlight the newly focused settings item.
            var settingsItems = this.GetSettingsItems(playerContainer, tracker);
            settingsItems[tracker.SettingsFocusIndex].AddToClassList("is_focus");
        }

        private VisualElement[] GetSettingsItems(PlayerContainer playerContainer, PlayerTracker tracker)
        {
            // Use the layout that matches this player's haptics capability.
            return tracker.SupportsRumbleHaptics
                ? playerContainer.SettingsItemsWithHaptics
                : playerContainer.SettingsItemsWithoutHaptics;
        }

        private void ClampSettingsFocus(PlayerContainer playerContainer, PlayerTracker tracker)
        {
            // Keep focus index inside visible settings item bounds.
            int lastSettingsIndex = this.GetSettingsItems(playerContainer, tracker).Length - 1;
            tracker.SettingsFocusIndex = Mathf.Clamp(tracker.SettingsFocusIndex, 0, lastSettingsIndex);
        }
    }
}
