using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class NetworkedSoundEmitter : NetworkBehaviour
{
    [SerializeField] private AudioClip oneShotClip;
    [SerializeField] private AudioClip loopClip;
    [SerializeField] private float cooldownSeconds = 0.2f;

    private AudioSource _source;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
    }

    private void Update()
    {
        if (!IsOwner || !IsClient)
            return;

        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            RequestPlaySoundServerRpc(true);
        }
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            RequestPlaySoundServerRpc(false);
        }
        else if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            RequestStopSoundServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPlaySoundServerRpc(bool oneShot, ServerRpcParams rpcParams = default)
    {
        PlaySoundClientRpc(oneShot);
    }

    [ClientRpc]
    private void PlaySoundClientRpc(bool oneShot, ClientRpcParams rpcParams = default)
    {
        if (oneShot)
            _source.PlayOneShot(oneShotClip);
        else
        {
            _source.clip = loopClip;
            _source.Play();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestStopSoundServerRpc(ServerRpcParams rpcParams = default)
    {
        StopSoundClientRpc();
    }

    [ClientRpc]
    private void StopSoundClientRpc(ClientRpcParams rpcParams = default)
    {
        _source.Stop();
    }
}
