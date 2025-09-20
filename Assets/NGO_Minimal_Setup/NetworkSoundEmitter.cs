using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using FMODUnity;
using Cadenza;
using FMOD.Studio;

public class NetworkedSoundEmitter : NetworkBehaviour
{
    private const string FMODParam_ID = "ID";
    [SerializeField] private EventReference oneShotEvent;
    [SerializeField] private EventReference playerUniqueEvent;

    [Header("Input Keys")]
    [SerializeField] private Key[] oneShotTriggerKeys;

    private bool created;
    private EventInstance instance;
    private bool isPlaying;

    private void Update()
    {
        if (!IsOwner || !IsClient)
            return;

        // One-shot clip palette
        for (int i = 0; i < this.oneShotTriggerKeys.Length; i++)
        {
            Key key = this.oneShotTriggerKeys[i];
            if (Keyboard.current[key].wasPressedThisFrame)
                RequestPlayOneShotServerRpc(i);
        }

        // Loop clip
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            RequestPlaySoundServerRpc();

        else if (Keyboard.current.spaceKey.wasReleasedThisFrame)
            RequestStopSoundServerRpc();
    }

    [ServerRpc]
    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void RequestPlayOneShotServerRpc(int index)
    {
        PlayOneShotClientRpc(index);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayOneShotClientRpc(int index)
    {
        AudioSystem.PlayOneShotWithParameter(this.oneShotEvent, FMODParam_ID, index);
    }

    [ServerRpc]
    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void RequestPlaySoundServerRpc()
    {
        PlaySoundClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlaySoundClientRpc()
    {
        if (!this.created)
        {
            this.instance = RuntimeManager.CreateInstance(this.playerUniqueEvent);
            RuntimeManager.AttachInstanceToGameObject(this.instance, this.transform, GetComponent<Rigidbody>());
            this.instance.setParameterByName("PlayerID", this.OwnerClientId);
            this.created = true;
        }

        if (!this.isPlaying)
        {
            this.instance.start();
            this.isPlaying = true;
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void RequestStopSoundServerRpc()
    {
        StopSoundClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void StopSoundClientRpc()
    {
        if (created && isPlaying)
        {
            instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            isPlaying = false;
        }
    }
}
