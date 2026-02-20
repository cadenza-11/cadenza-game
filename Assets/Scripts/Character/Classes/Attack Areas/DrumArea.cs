using UnityEngine;


namespace Cadenza
{
    public class DrumArea : MonoBehaviour, IAttackArea
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

        public void StartLightAttack(Character character)
        {
            character.comboM.ProcessCombo(AttkTypes.Light, out var reward);
            character.ManageAttackDirection();

            int flowDamage = character.HasFlowBuff(2) ? 1 : 0;
            float damageModifier = character.baseLightDamage / 2 * flowDamage * reward.Multiplier;

            this.damage = (int)(character.baseLightDamage + (character.baseLightDamage * damageModifier)); // TEMP: should be flaot.
            this.comboMove = reward.AttackEffect;
            this.gameObject.SetActive(true);
        }

        public void StartHeavyAttack(Character character)
        {
            character.comboM.ProcessCombo(AttkTypes.Heavy, out var reward);
            character.ManageAttackDirection();

            int flowDamage = character.HasFlowBuff(2) ? 1 : 0;
            float damageModifier = character.baseHeavyDamage / 2 * flowDamage * reward.Multiplier;

            this.damage = (int)(character.baseHeavyDamage + (character.baseHeavyDamage * damageModifier)); // TEMP: should be flaot.
            this.comboMove = reward.AttackEffect;
            this.gameObject.SetActive(true);
        }

        public void EndAttack()
        {
            this.gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider collider)
        {
            if (collider.gameObject.TryGetComponent(out Enemy enemy))
                enemy.DoDamage(this.damage);

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

