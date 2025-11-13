using UnityEngine;

public class AttackArea : MonoBehaviour
{
    public int damage = 0;
    private GameObject go = null;

    public void SetActive(bool enabled)
    {
        if (this.go == null)
            this.go = this.gameObject;

        this.go.SetActive(enabled);
    }


    private void OnTriggerEnter(Collider collider)
    {
        Debug.Log("dealt" + this.damage + "damage");
    }
}
