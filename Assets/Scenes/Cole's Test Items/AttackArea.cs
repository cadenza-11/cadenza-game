using UnityEngine;

public class AttackArea : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private int damage = 3;

    private void OnTriggerEnter(Collider collider)
    {
        Debug.Log("dealt" + damage + "damage");
    }
}
