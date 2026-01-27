using Cadenza;
using UnityEngine;

/// <summary>
/// Designed to make the combo system easier to read. In description, L is light and H is heavy.
/// </summary>

public class AttackArea : MonoBehaviour
{
    public int damage = 0;
    public float knockbackScale;
    public AttkEffect comboMove = AttkEffect.None;
    private GameObject go = null;

    public void SetActive(bool enabled)
    {
        if (this.go == null)
            this.go = this.gameObject;

        this.go.SetActive(enabled);
    }


    private void OnTriggerEnter(Collider collider)
    {
        if (collider.CompareTag("Player"))
        {
            Character hitEntity = collider.gameObject.GetComponent<Character>();
            hitEntity.DoDamage(this.damage);
        }
        if (collider.CompareTag("Enemy"))
        {
            Enemy hitEntity = collider.gameObject.GetComponent<Enemy>();
            hitEntity.DoDamage(2);
        }

        Vector3 direction = collider.transform.position - this.transform.position;
        Vector3 force = direction.normalized * this.knockbackScale;
        force.y = 1f; // Add a small upward knockback.
        collider.attachedRigidbody.AddForce(force, ForceMode.Impulse);

        //Check back with this code in case you want to impliment knockback
        /*
        switch (this.comboMove)
        {
            case AttkEffect.Light_Knockback:
                Vector3 lightDirection = this.transform.position - collider.transform.position;
                Vector3 lightNormalDirection = lightDirection.normalized;
                collider.attachedRigidbody.AddForce(lightNormalDirection * -3.0f, ForceMode.Impulse);
                break;

            case AttkEffect.Area_Smash:
                Vector3 areaDirection = this.transform.position - collider.transform.position;
                Vector3 areaNormalDirection = areaDirection.normalized;
                collider.attachedRigidbody.AddForce(areaNormalDirection * -3.0f, ForceMode.Impulse);
                break;

            case AttkEffect.Heavy_Knockback:
                Vector3 heavyDirection = this.transform.position - collider.transform.position;
                Vector3 heavyNormalDirection = heavyDirection.normalized;
                collider.attachedRigidbody.AddForce(heavyNormalDirection * -6.0f, ForceMode.Impulse);
                break;

            case AttkEffect.Base_Smash:
                Vector3 baseDirection = this.transform.position - collider.transform.position;
                Vector3 baseNormalDirection = baseDirection.normalized;
                collider.attachedRigidbody.AddForce(baseNormalDirection * -4.0f, ForceMode.Impulse);
                break;

        }
        */
        this.comboMove = AttkEffect.None;
    }
}
