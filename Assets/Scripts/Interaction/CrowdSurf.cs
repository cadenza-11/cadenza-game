using System.Collections.Generic;
using Cadenza;
using UnityEngine;

public class CrowdSurf : MonoBehaviour
{
    [SerializeField] private Vector3 targetPosition;
    private List<GameObject> crowdSurfers = new List<GameObject>();

    void Update()
    {
        foreach (GameObject surfer in this.crowdSurfers)
        {
            if (surfer != null)
            {
                Vector3 direction = (this.targetPosition - surfer.transform.position).normalized;
                surfer.GetComponent<Rigidbody>().AddForce(direction * 100f, ForceMode.Acceleration);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player") || other.gameObject.CompareTag("Enemy"))
        {
            if (!this.crowdSurfers.Contains(other.gameObject))
                this.crowdSurfers.Add(other.gameObject);
            if (other.gameObject.CompareTag("Player"))
                other.gameObject.GetComponent<Character>().OnCrowdSurf(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (this.crowdSurfers.Contains(other.gameObject))
            this.crowdSurfers.Remove(other.gameObject);
        if (other.gameObject.CompareTag("Player"))
            other.gameObject.GetComponent<Character>().OnCrowdSurf(false);
    }
}
