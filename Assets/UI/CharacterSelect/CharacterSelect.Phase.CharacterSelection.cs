using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Cadenza
{
    public partial class CharacterSelect
    {
        private sealed class CharacterSelectionPhaseHandler : PhaseHandler
        {
            public CharacterSelectionPhaseHandler(CharacterSelect owner)
                : base(owner)
            {
            }

            public override void OnSubmit(Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
                if (this.Owner.TrySelectCharacter(player))
                    tracker.Phase = SelectPhase.Settings;
            }

            public override void OnCancel(Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
                PlayerSystem.RemovePlayer(player);
                tracker.Phase = SelectPhase.Joining;
            }

            public override void OnNavigate(MoveDirection moveDirection, Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
                VisualElement container = playerContainer.CharacterSelectionContainer;
                VisualElement characterPicker = container.Q<VisualElement>("c_CharacterPicker");
                VisualElement cosmeticsPicker = container.Q<VisualElement>("c_CosmeticsPicker");

                // Move left or right.
                if (moveDirection == MoveDirection.Left || moveDirection == MoveDirection.Right)
                {
                    // Select cosmetics.
                    if (cosmeticsPicker.ClassListContains("is_focus"))
                    {
                        // TODO: Implement //
                        Debug.LogWarning("There are no cosmetics.");
                        return;
                    }

                    // Select character.
                    string currentChar = container.Q<Label>("update_CharacterName").text;
                    var shownCharacter = moveDirection == MoveDirection.Right
                        ? this.Owner.classManager.GetNextCharacter(currentChar)
                        : this.Owner.classManager.GetPreviousCharacter(currentChar);

                    this.Owner.ChangeShownCharacter(container, shownCharacter);
                }

                // Move up or down.
                else if (moveDirection == MoveDirection.Up || moveDirection == MoveDirection.Down)
                {
                    // Switch between selecting character or selecting cosmetics.
                    characterPicker.ToggleInClassList("is_focus");
                    cosmeticsPicker.ToggleInClassList("is_focus");
                }
            }
        }

        private void ChangeShownCharacter(VisualElement characterSelectContainer, UIClassManager.CharacterSelectInfo shownClass)
        {
            // Update label.
            characterSelectContainer.Q<Label>("update_CharacterName").text = shownClass.Class.Name;

            // Update portrait.
            VisualElement portrait = characterSelectContainer.Q<VisualElement>("portrait_Character");
            portrait.style.backgroundImage = shownClass.Class.Portrait;

            // Update taken status.
            VisualElement selector = characterSelectContainer.Q<VisualElement>("c_CharacterPicker");
            if (shownClass.IsTaken)
                selector.AddToClassList("taken");
            else
                selector.RemoveFromClassList("taken");
        }

        private void RefreshShownCharacters()
        {
            // Update UI whenever a character is taken.
            foreach (var container in this.playerContainers)
            {
                string currentClassName = container.CharacterSelectionContainer.Q<Label>("update_CharacterName").text;
                var currentCharacter = this.classManager.GetCharacter(currentClassName);
                this.ChangeShownCharacter(container.CharacterSelectionContainer, currentCharacter);
            }
        }

        private bool TrySelectCharacter(Player player)
        {
            var playerContainer = this.playerContainers[player.ID];

            CharacterClass selectedCharacter = this.classManager.SelectCharacter
            (
                player,
                playerContainer.CharacterSelectionContainer.Q<Label>("update_CharacterName").text
            );

            player.SetCharacterClass(selectedCharacter);
            AudioSystem.SetInstrumentActive(player.CharacterClass, true);

            return selectedCharacter != null;
        }

        private void DeselectCharacter(Player player)
        {
            AudioSystem.SetInstrumentActive(player.CharacterClass, false);
            player.SetCharacterClass(null);
            this.classManager.UnselectCharacter(player);
        }

        private void OnPlayerAdded(Player player)
        {
            // Reset the tracker.
            var tracker = this.playerTrackers[player];
            tracker.Phase = SelectPhase.CharacterSelection;
            tracker.SettingsFocusIndex = 0;
            tracker.HapticsFocusIndex = 0;

            // Reset calibration.
            this.ResetCalibration(player);

            // Reset shown character.
            var container = this.playerContainers[player.ID];
            this.ChangeShownCharacter(container.CharacterSelectionContainer, this.classManager.GetNextCharacter(""));
            container.CharacterSelectionContainer.Q<VisualElement>("c_CharacterPicker").AddToClassList("is_focus");
            container.CharacterSelectionContainer.Q<VisualElement>("c_CosmeticsPicker").RemoveFromClassList("is_focus");

            // Set player settings.
            container.Container.Q<Label>("txt_PlayerName").text = player.Name;
            this.UpdateHapticsAvailability(player);
            this.SetHapticsStrength(player, tracker.HapticsStrength);
            this.SetHapticsDuration(player, tracker.HapticsDuration);
            this.SetSettingsFocus(container, tracker);
            this.SetHapticsFocus(container, tracker.HapticsFocusIndex);

            // Advance phase.
            this.ShowPhase(container, SelectPhase.CharacterSelection);
        }

        private void OnPlayerRemoved(Player player)
        {
            // Reset the tracker.
            var tracker = this.playerTrackers[player];
            tracker.Phase = SelectPhase.Joining;

            // Reset calibration.
            this.ResetCalibration(player);

            // Reset shown character.
            var container = this.playerContainers[player.ID];
            this.ChangeShownCharacter(container.CharacterSelectionContainer, this.classManager.GetNextCharacter(""));
            container.CharacterSelectionContainer.Q<VisualElement>("c_CharacterPicker").AddToClassList("is_focus");
            container.CharacterSelectionContainer.Q<VisualElement>("c_CosmeticsPicker").RemoveFromClassList("is_focus");

            // Show default phase.
            this.ShowPhase(container, SelectPhase.Joining);
        }
    }
}
