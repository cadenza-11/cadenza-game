using UnityEngine;


namespace Cadenza
{
    public class DrumArea : MonoBehaviour, IAttackArea
    {
        public int damage = 0;
        public float knockbackScale;
        public AttkEffect comboMove = AttkEffect.None;
        private GameObject go = null;
        private ScoreDef? attackScore;

        public void SetActive(bool enabled)
        {
            if (this.go == null)
                this.go = this.gameObject;

            this.go.SetActive(enabled);
        }

        public void StartLightAttack(Character character)
        {
            this.attackScore = character.input.lightAttack;
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
            this.attackScore = character.input.heavyAttack;
            character.comboM.ProcessCombo(AttkTypes.Heavy, out var reward);
            character.ManageAttackDirection();

            int flowDamage = character.HasFlowBuff(2) ? 1 : 0;
            float damageModifier = character.baseHeavyDamage / 2 * flowDamage * reward.Multiplier;

            this.damage = (int)(character.baseHeavyDamage + (character.baseHeavyDamage * damageModifier)); // TEMP: should be flaot.
            this.knockbackScale = reward.Knockback;
            this.comboMove = reward.AttackEffect;
            this.gameObject.SetActive(true);
        }

        public void EndAttack()
        {
            this.attackScore = null;
            this.gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider collider)
        {
            // Hit enemy.
            if (collider.gameObject.TryGetComponent(out Enemy enemy))
            {
                enemy.DoDamage(this.damage);

                // Add knockback.
                Vector3 direction = collider.transform.position - this.transform.position;
                Vector3 force = direction.normalized * this.knockbackScale;
                force.y = 2f;
                collider.attachedRigidbody.AddForce(force, ForceMode.Impulse);
            }

            // Hit ally.
            else if (collider.gameObject.TryGetComponent(out Character character) && this.attackScore.HasValue)
            {
                character.OnAllyHit(this.attackScore.Value);
            }
        }
    }
}

