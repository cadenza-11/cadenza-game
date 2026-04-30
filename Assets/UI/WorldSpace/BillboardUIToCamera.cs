using UnityEngine;

public class BillboardUIToCamera : MonoBehaviour
{
    private Transform cameraTransform;
    void Start()
    {
        if (Camera.main != null)
            this.cameraTransform = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (this.cameraTransform != null)
        {
            this.transform.LookAt
            (
                this.transform.position + this.cameraTransform.rotation * Vector3.forward,
                this.cameraTransform.rotation * Vector3.up
            );
        }
    }
}
