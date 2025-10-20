using UnityEngine;

public class AttackArea : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public int damage = 0;

    private void OnTriggerEnter(Collider collider)
    {
        Debug.Log("dealt" + this.damage + "damage");
    }
}
