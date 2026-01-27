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

        // Stop current horizontal movement.
        // Vector3 v = collider.attachedRigidbody.linearVelocity;
        // v.x = 0;
        // v.z = 0;
        // collider.attachedRigidbody.linearVelocity = v;

        // Add knockback.
        Vector3 direction = collider.transform.position - this.transform.position;
        Vector3 force = direction.normalized * this.knockbackScale;
        force.y = 2f;
        collider.attachedRigidbody.AddForce(force, ForceMode.Impulse);

        this.comboMove = AttkEffect.None;
    }
}
