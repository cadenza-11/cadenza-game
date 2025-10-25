using Unity.Cinemachine;
using UnityEngine;

namespace Cadenza
{
    public class CinemachineTargetUpdater : ApplicationSystem
    {
        [SerializeField] CinemachineTargetGroup cinemachineTargetComponent;
        public override void OnGameStart()
        {
            GameStateManager.OnGameStateChanged += this.OnGameStateChanged;
        }

        public override void OnGameStop()
        {
            GameStateManager.OnGameStateChanged -= this.OnGameStateChanged;
        }

        public void OnGameStateChanged(GameStateManager.GameState state)
        {
            this.cinemachineTargetComponent.Targets.Clear();
            if (state == GameStateManager.GameState.InLevel)
            {
                foreach (int deviceID in PlayerSystem.PlayerRoster)
                {
                    if (PlayerSystem.TryGetPlayerByID(deviceID, out Player player))
                        this.cinemachineTargetComponent.AddMember(player.transform, 1f, .5f);
                }
            }
        }
    }
}