using UnityEngine;

public class AttackArea : MonoBehaviour
{
    public int damage = 0;
    public int comboMove = 0;
    private GameObject go = null;
    public bool projDirection;

    public void SetActive(bool enabled)
    {
        if (this.go == null)
            this.go = this.gameObject;

        this.go.SetActive(enabled);
    }


    private void OnTriggerEnter(Collider collider)
    {
        Debug.Log("dealt" + this.damage + "damage");
        switch (this.comboMove)
        {
            case 1:
                this.comboMove = 0;
                Vector3 lightDirection = this.transform.position - collider.transform.position;
                Vector3 lightNormalDirection = lightDirection.normalized;
                collider.attachedRigidbody.AddForce(lightNormalDirection * -3.0f, ForceMode.Impulse);
                break;

            case 4:
                this.comboMove = 0;
                Vector3 heavyDirection = this.transform.position - collider.transform.position;
                Vector3 heavyNormalDirection = heavyDirection.normalized;
                collider.attachedRigidbody.AddForce(heavyNormalDirection * -6.0f, ForceMode.Impulse);
                break;

        }


    }
}
