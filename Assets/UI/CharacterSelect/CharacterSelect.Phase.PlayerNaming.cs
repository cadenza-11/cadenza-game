using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Cadenza
{
    public partial class CharacterSelect
    {
        private sealed class PlayerNamingPhaseHandler : PhaseHandler
        {
            public PlayerNamingPhaseHandler(CharacterSelect owner)
                : base(owner)
            {
            }

            public override void OnSubmit(Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
                // Route submit through the on-screen keyboard.
                playerContainer.Keyboard.OnSubmit();
            }

            public override void OnCancel(Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
                // Route cancel through the on-screen keyboard.
                playerContainer.Keyboard.OnCancel();
            }

            public override void OnNavigate(MoveDirection moveDirection, Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
                // Route directional navigation through the keyboard.
                playerContainer.Keyboard.OnNavigate(moveDirection);
            }
        }

        private void OnSubmitName(PlayerContainer playerContainer)
        {
            // Find the player backing this UI container.
            for (int i = 0; i < this.playerContainers.Length; i++)
            {
                if (this.playerContainers[i] == playerContainer &&
                    PlayerSystem.TryGetPlayerByID(i, out Player player))
                {
                    var tracker = this.playerTrackers[player];
                    if (tracker.Phase == SelectPhase.PlayerNaming)
                    {
                        // Don't allow empty names.
                        var name = playerContainer.Keyboard.value;
                        if (name != string.Empty)
                        {
                            // Set name.
                            player.Name = name;
                            playerContainer.Container.Q<Label>("txt_PlayerName").text = name;
                        }

                        // Set phase.
                        this.playerTrackers[player].Phase = SelectPhase.Settings;
                        this.ShowPhase(playerContainer, tracker.Phase);
                    }
                }
            }
        }

        private void OnCancelName(PlayerContainer playerContainer)
        {
            // Find the player backing this UI container.
            for (int i = 0; i < this.playerContainers.Length; i++)
            {
                if (this.playerContainers[i] == playerContainer &&
                    PlayerSystem.TryGetPlayerByID(i, out Player player))
                {
                    var tracker = this.playerTrackers[player];
                    if (tracker.Phase == SelectPhase.PlayerNaming)
                    {
                        // Set phase.
                        this.playerTrackers[player].Phase = SelectPhase.Settings;
                        this.ShowPhase(playerContainer, tracker.Phase);
                    }
                }
            }
        }

        private void BeginRenaming(Player player)
        {
            if (!this.playerTrackers.TryGetValue(player, out PlayerTracker tracker))
                return;

            // Enter naming phase and seed keyboard with the current player name.
            tracker.Phase = SelectPhase.PlayerNaming;
            this.playerContainers[player.ID].Keyboard.value = player.Name;
        }
    }
}
