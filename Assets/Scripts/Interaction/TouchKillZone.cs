using UnityEngine;

public class TouchKillZone : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(UnityEngine.Collider other)
    {
        Debug.Log("Collision Enter");
        other.attachedRigidbody.transform.position = Vector3.zero;
    }

    private void OnTriggerStay(UnityEngine.Collider other)
    {
        Debug.Log("Collision Stay");
        other.attachedRigidbody.transform.position = Vector3.zero;
    }
}
