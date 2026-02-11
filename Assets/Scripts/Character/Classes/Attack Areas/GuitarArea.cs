using UnityEngine;


namespace Cadenza
{
    public class GuitarArea : MonoBehaviour, IAttackArea
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
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

        public void StartAttack(Character character)
        {
            character.comboM.ProcessCombo(AttkTypes.Light, out var reward);
            character.ManageAttackDirection();

            int flowDamage = character.HasFlowBuff(2) ? 1 : 0;
            float damageModifier = character.baseLightDamage / 2 * flowDamage * reward.Multiplier;

            this.damage = (int)(character.baseLightDamage * damageModifier); // TEMP: should be flaot.
            this.comboMove = reward.AttackEffect;
            this.gameObject.SetActive(true);
        }

        public void EndAttack()
        {
            this.gameObject.SetActive(false);
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
}
