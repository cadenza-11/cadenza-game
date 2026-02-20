using UnityEngine;

public class TouchKillZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        other.attachedRigidbody.transform.position = Vector3.zero;
    }
}
