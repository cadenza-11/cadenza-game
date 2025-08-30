using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class NetworkedSoundEmitter : NetworkBehaviour
{
    [Header("One Shots")]
    [SerializeField] private Key[] oneShotTriggerKeys;
    [SerializeField] private AudioClip[] oneShotClips;

    [Header("Loop Sound (play with spacebar)")]
    [SerializeField] private AudioClip[] ownerUniqueSound;

    [Header("Metronome Sound (play with enter)")]
    [SerializeField] private AudioClip metronomeSound;

    private AudioSource _source;
    private bool isMetronomeEnabled;

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

        // Metronome
        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            if (!this.isMetronomeEnabled)
            {
                RequestPlayMetronomeServerRpc();
                this.isMetronomeEnabled = true;
            }
            else
            {
                RequestStopSoundServerRpc();
                this.isMetronomeEnabled = false;
            }
        }
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

    // Metronome
    [ServerRpc]
    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void RequestPlayMetronomeServerRpc()
    {
        PlayMetronomeClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayMetronomeClientRpc()
    {
        _source.clip = this.metronomeSound;
        _source.Play();
    }
}
