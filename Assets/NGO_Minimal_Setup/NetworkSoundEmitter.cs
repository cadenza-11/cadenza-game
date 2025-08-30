using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class NetworkedSoundEmitter : NetworkBehaviour
{
    [SerializeField] private Key[] oneShotTriggerKeys;
    [SerializeField] private AudioClip[] oneShotClips;
    [SerializeField] private AudioClip[] ownerUniqueSound;
    [SerializeField] private float cooldownSeconds = 0.2f;

    private AudioSource _source;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _source = GetComponent<AudioSource>();
    }

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
        if (index < this.oneShotClips.Length)
            _source.PlayOneShot(this.oneShotClips[index]);
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
        _source.clip = this.ownerUniqueSound[this.OwnerClientId];
        _source.Play();
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void RequestStopSoundServerRpc()
    {
        StopSoundClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void StopSoundClientRpc()
    {
        _source.Stop();
    }
}
