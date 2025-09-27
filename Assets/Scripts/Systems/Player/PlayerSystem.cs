using System.Collections.Generic;
using UnityEngine;

namespace Cadenza
{
    public class PlayerSystem : ApplicationSystem, CadenzaActions.IPlayerActions
    {
        [Header("Test values")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private float playerSpeed;

        private Dictionary<int, GameObject> playersByID;
        private Dictionary<int, Vector3> playerFrameImpulsesByID;

        public override void OnInitialize()
        {
            this.playersByID = new();
            this.playerFrameImpulsesByID = new();

            InputSystem.PlayerInputMap.AddCallbacks(this);
        }

        public void OnAttack(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
        }

        public void OnInteract(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
        }

        public void OnMove(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            int deviceID = context.control.device.deviceId;
            if (!this.playersByID.ContainsKey(deviceID))
            {
                GameObject newPlayer = Instantiate(this.playerPrefab);
                this.playersByID[deviceID] = newPlayer;
            }

            var input = context.performed ? context.ReadValue<Vector2>() : Vector2.zero;
            this.playerFrameImpulsesByID[deviceID] = new Vector3(input.x, 0, input.y);
        }

        public override void OnUpdate()
        {
            foreach ((int id, var player) in this.playersByID)
                player.transform.Translate(this.playerSpeed * Time.deltaTime * this.playerFrameImpulsesByID[id]);
        }
    }
}