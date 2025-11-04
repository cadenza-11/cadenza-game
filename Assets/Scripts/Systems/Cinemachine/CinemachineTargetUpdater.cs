using Unity.Cinemachine;
using UnityEngine;

namespace Cadenza
{
    public class CinemachineTargetUpdater : ApplicationSystem
    {
        [SerializeField] CinemachineTargetGroup cinemachineTargetComponent;
        public override void OnGameStart()
        {
            foreach (var id in PlayerSystem.PlayerRoster)
            {
                if (PlayerSystem.TryGetPlayerByID(id, out Player player))
                    this.cinemachineTargetComponent.AddMember(player.transform, 1f, .5f);
            }
        }
    }
}