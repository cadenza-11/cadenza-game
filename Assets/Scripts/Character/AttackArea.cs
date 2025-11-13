using UnityEngine;

public class AttackArea : MonoBehaviour
{
    public int damage = 0;
    public int comboMove = 0;
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
        if(this.comboMove == 1)
        {
            this.comboMove = 0;
            Vector3 direction = this.transform.position - collider.transform.position;
            Vector3 normalDirection = direction.normalized;
            collider.attachedRigidbody.AddForce(normalDirection * -10.0f, ForceMode.Impulse);
        }
    }
}
