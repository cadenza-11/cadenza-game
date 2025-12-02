using System.Collections.Generic;
using UnityEngine;

namespace Cadenza
{
    /// <summary>
    /// Add this component to a GameObject to make it interactable when
    /// a player enters its range. The attached GameObject must have some script
    /// that implements the IInteractable interface.
    /// </summary>
    [RequireComponent(typeof(Collider), typeof(IInteractable))]
    public class Interactable : MonoBehaviour
    {
        private IInteractable interactable;
        private readonly List<Player> interactingPlayers = new();

        void Start()
        {
            this.interactable = this.GetComponent<IInteractable>();
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out Character character))
            {
                character.Player.RegisterInteract(this.interactable);
                this.interactingPlayers.Add(character.Player);
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out Character character))
            {
                character.Player.UnregisterInteract(this.interactable);
                this.interactingPlayers.Add(character.Player);
            }
        }

        void OnDestroy()
        {
            foreach (var player in this.interactingPlayers)
                player.UnregisterInteract(this.interactable);
        }
    }
}
