using Unity.Cinemachine;
using UnityEngine;

namespace Cadenza
{
    public class CinemachineTargetUpdater : ApplicationSystem
    {
        [SerializeField] CinemachineTargetGroup cinemachineTargetComponent;
        public override void OnGameStart()
        {
            foreach (var player in PlayerSystem.PlayersByID.Values)
                this.cinemachineTargetComponent.AddMember(player.Character.transform, 1f, .5f);
        }
    }
}
