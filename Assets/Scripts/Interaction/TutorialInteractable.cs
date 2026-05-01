using System.Collections.Generic;
using UnityEngine;

namespace Cadenza
{
    [RequireComponent(typeof(Interactable))]
    public class TutorialInteractable : MonoBehaviour, IInteractable
    {
        private readonly HashSet<Player> activePlayers = new();

        public void OnInteract(Player player)
        {
            if (player == null)
                return;

            this.activePlayers.Add(player);
            UISystem.FindPanel<TutorialPanel>()?.NextPage();
        }
    }
}
