using Unity.Cinemachine;
using UnityEngine;

namespace Cadenza
{
    /// <summary>
    /// Adds all current Players to a Cinemachine group that will be followed by a virtual camera.
    /// </summary>
    [RequireComponent(typeof(CinemachineTargetGroup))]
    public class CinemachineTargetUpdater : MonoBehaviour
    {
        [SerializeField] private float defaultTargetWeight;
        [SerializeField] private float defaultTargetRadius;

        void Start()
        {
            var group = this.GetComponent<CinemachineTargetGroup>();

            foreach (var player in PlayerSystem.Players)
                group.AddMember(player.Character.transform, this.defaultTargetWeight, this.defaultTargetRadius);

            PlayerSystem.PlayerSpawned += p => group.AddMember(p.Character.transform, this.defaultTargetWeight, this.defaultTargetRadius);
        }
    }
}
