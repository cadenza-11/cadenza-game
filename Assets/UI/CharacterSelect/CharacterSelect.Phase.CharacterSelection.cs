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
                {
                    tracker.Phase = SelectPhase.Settings;
                    player.SetColorway(this.Owner.GetSelectedColorway(tracker));
                }
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
                        if (moveDirection == MoveDirection.Right)
                            tracker.SelectedColorIndex = tracker.SelectedColorIndex + 1 == this.Owner.colorways.Length ? 0 : tracker.SelectedColorIndex + 1;
                        else
                            tracker.SelectedColorIndex = tracker.SelectedColorIndex - 1 < 0 ? this.Owner.colorways.Length - 1 : tracker.SelectedColorIndex - 1;

                        this.Owner.RefreshShownColor(playerContainer, tracker);

                        return;
                    }

                    // Select character.
                    string currentChar = container.Q<Label>("update_CharacterName").text;
                    var shownCharacter = moveDirection == MoveDirection.Right
                        ? this.Owner.classManager.GetNextCharacter(currentChar)
                        : this.Owner.classManager.GetPreviousCharacter(currentChar);

                    this.Owner.ChangeShownCharacter(container, shownCharacter, this.Owner.GetSelectedColorway(tracker));
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

        private void ChangeShownCharacter(VisualElement characterSelectContainer, UIClassManager.CharacterSelectInfo shownClass, Colorway selectedColorway)
        {
            // Update label.
            characterSelectContainer.Q<Label>("update_CharacterName").text = shownClass.Class.Name;

            // Update portrait.
            VisualElement portrait = characterSelectContainer.Q<VisualElement>("portrait_Character");
            MaterialDefinition portraitMaterial = portrait.resolvedStyle.unityMaterial;
            portraitMaterial.SetTexture("_Portrait", shownClass.Class.FullBodyPortrait);
            portraitMaterial.SetColor("_PrimaryColor", (selectedColorway == null) ? this.colorways[0].PrimaryColor : selectedColorway.PrimaryColor);
            portraitMaterial.SetColor("_SecondaryColor", (selectedColorway == null) ? this.colorways[0].SecondaryColor : selectedColorway.SecondaryColor);
            portraitMaterial.SetColor("_TertiaryColor", (selectedColorway == null) ? this.colorways[0].TertiaryColor : selectedColorway.TertiaryColor);
            portrait.style.backgroundImage = shownClass.Class.FullBodyPortrait;
            portrait.style.backgroundColor = (selectedColorway == null) ? this.colorways[0].SecondaryColor : selectedColorway.SecondaryColor;

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
                Colorway colors = (Colorway)container.CharacterSelectionContainer.Q<VisualElement>("display_Color").dataSource;
                string currentClassName = container.CharacterSelectionContainer.Q<Label>("update_CharacterName").text;
                var currentCharacter = this.classManager.GetCharacter(currentClassName);
                this.ChangeShownCharacter(container.CharacterSelectionContainer, currentCharacter, colors);
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
            AudioSystem.SetParameter(player.CharacterClass.Name, 1.0f);

            return selectedCharacter != null;
        }

        private void DeselectCharacter(Player player)
        {
            AudioSystem.SetParameter(player.CharacterClass.Name, 0.0f);
            player.SetCharacterClass(null);
            this.classManager.UnselectCharacter(player);
        }

        private void ChangeShownColor(VisualElement characterSelectContainer, Colorway color)
        {
            var display = characterSelectContainer.Q<VisualElement>("display_Color");
            display.dataSource = color;
            var bgColor = color.SecondaryColor;
            bgColor.a = 1;
            display.style.backgroundColor = bgColor;
            display.style.backgroundImage = color.DisplayImage;
            if (this.IsVisible)
                this.ChangeShownCharacter(characterSelectContainer, this.classManager.GetCharacter(characterSelectContainer.Q<Label>("update_CharacterName").text), color);
        }

        private Colorway GetSelectedColorway(PlayerTracker tracker)
        {
            tracker.SelectedColorIndex = Mathf.Clamp(tracker.SelectedColorIndex, 0, this.colorways.Length - 1);
            return this.colorways[tracker.SelectedColorIndex];
        }

        private void RefreshShownColor(PlayerContainer playerContainer, PlayerTracker tracker)
        {
            this.ChangeShownColor(
                playerContainer.CharacterSelectionContainer,
                this.GetSelectedColorway(tracker)
            );
        }

        private void OnPlayerAdded(Player player)
        {
            // Reset the tracker.
            var tracker = this.playerTrackers[player];
            tracker.Phase = SelectPhase.CharacterSelection;
            tracker.SettingsFocusIndex = 0;
            tracker.HapticsFocusIndex = 0;
            tracker.SelectedColorIndex = 0;

            // Reset calibration.
            this.ResetCalibration(player);

            // Reset shown character.
            var container = this.playerContainers[player.ID];
            this.ChangeShownCharacter(container.CharacterSelectionContainer, this.classManager.GetNextCharacter(""), this.colorways[0]);
            container.CharacterSelectionContainer.Q<VisualElement>("c_CharacterPicker").AddToClassList("is_focus");
            container.CharacterSelectionContainer.Q<VisualElement>("c_CosmeticsPicker").RemoveFromClassList("is_focus");
            this.RefreshShownColor(container, tracker);

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
            tracker.SelectedColorIndex = 0;

            // Reset calibration.
            this.ResetCalibration(player);

            // Reset shown character.
            var container = this.playerContainers[player.ID];
            this.ChangeShownCharacter(container.CharacterSelectionContainer, this.classManager.GetNextCharacter(""), this.colorways[0]);
            container.CharacterSelectionContainer.Q<VisualElement>("c_CharacterPicker").AddToClassList("is_focus");
            container.CharacterSelectionContainer.Q<VisualElement>("c_CosmeticsPicker").RemoveFromClassList("is_focus");
            container.CharacterSelectionContainer.Q<VisualElement>("c_CosmeticsPicker").RemoveFromClassList("is_focus");
            this.RefreshShownColor(container, tracker);

            // Show default phase.
            this.ShowPhase(container, SelectPhase.Joining);
        }
    }
}
