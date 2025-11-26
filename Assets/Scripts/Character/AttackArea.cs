using UnityEngine;

/// <summary>
/// Designed to make the combo system easier to read. In description, L is light and H is heavy.
/// </summary>
public enum AttkEffect
{
    None, //Basic effect of attack
    Light_Knockback, //Light knockback, performed after a L L L combo
    Projectile, //Shoots a projectile forward, performed after a L H L combo
    Area_Smash, //Larger but weaker ground slam, performed after a L H H combo
    Heavy_Knockback, //Shoots a projectile forward, performed after a H H L combo
    Base_Smash //Base AOE ground slam, performed after a H H H combo
}

public class AttackArea : MonoBehaviour
{
    public int damage = 0;
    public int comboMove = (int)AttkEffect.None;
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
        switch (this.comboMove)
        {
            case (int)AttkEffect.Light_Knockback:
                Vector3 lightDirection = this.transform.position - collider.transform.position;
                Vector3 lightNormalDirection = lightDirection.normalized;
                collider.attachedRigidbody.AddForce(lightNormalDirection * -3.0f, ForceMode.Impulse);
                break;

            case (int)AttkEffect.Area_Smash:
                Vector3 areaDirection = this.transform.position - collider.transform.position;
                Vector3 areaNormalDirection = areaDirection.normalized;
                collider.attachedRigidbody.AddForce(areaNormalDirection * -3.0f, ForceMode.Impulse);
                break;

            case (int)AttkEffect.Heavy_Knockback:
                Vector3 heavyDirection = this.transform.position - collider.transform.position;
                Vector3 heavyNormalDirection = heavyDirection.normalized;
                collider.attachedRigidbody.AddForce(heavyNormalDirection * -6.0f, ForceMode.Impulse);
                break;

            case (int)AttkEffect.Base_Smash:
                Vector3 baseDirection = this.transform.position - collider.transform.position;
                Vector3 baseNormalDirection = baseDirection.normalized;
                collider.attachedRigidbody.AddForce(baseNormalDirection * -4.0f, ForceMode.Impulse);
                break;

        }

        this.comboMove = (int)AttkEffect.None;
    }
}
