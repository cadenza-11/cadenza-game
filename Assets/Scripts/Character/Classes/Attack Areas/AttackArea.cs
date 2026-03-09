using UnityEngine;


namespace Cadenza
{
    public class AttackArea : MonoBehaviour
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


            this.damage = this.CalcDamageMod(character, reward, character.baseLightDamage); //should be float (maybe)
            this.knockbackScale = reward.Knockback;
            this.comboMove = reward.AttackEffect;
            this.gameObject.SetActive(true);
        }

        public void StartHeavyAttack(Character character)
        {
            this.attackScore = character.input.heavyAttack;
            character.comboM.ProcessCombo(AttkTypes.Heavy, out var reward);
            character.ManageAttackDirection();

            this.damage = this.CalcDamageMod(character, reward, character.baseHeavyDamage); //should be float (maybe)
            this.knockbackScale = reward.Knockback;
            this.comboMove = reward.AttackEffect;
            this.gameObject.SetActive(true);
        }

        public void EndAttack()
        {
            this.attackScore = null;
            this.gameObject.SetActive(false);
        }

        private int CalcDamageMod(Character character, Combo.ComboReward reward, float damage)
        {
            float scoreClassMod = 0;
            ScoreDef sDef = this.attackScore.Value;
            switch (sDef.Class)
            {
                case ScoreClass.Bad:
                    scoreClassMod = 0.5f;
                    break;
                case ScoreClass.OK:
                    scoreClassMod = 1;
                    break;
                case ScoreClass.Great:
                    scoreClassMod = 1.2f;
                    break;
                case ScoreClass.Perfect:
                    scoreClassMod = 1.5f;
                    break;
            }
            float flowDamage = (character.HasFlowBuff(2) ? 1 : 0) * 0.25f;
            float endDamage = damage * reward.Multiplier * (1 + flowDamage) * scoreClassMod;
            //Debug.Log("Base Damage: " + damage + ", Combo Multiplier: " + reward.Multiplier + ", Flow Multiplier: " + (1 + flowDamage) + ", Score Class Multiplier: " + scoreClassMod + ", Final Damage: " + endDamage);
            return (int)endDamage;
        }

        private void OnTriggerEnter(Collider collider)
        {
            // Hit enemy.
            if (collider.gameObject.TryGetComponent(out Enemy enemy))
            {
                enemy.TakeDamage(this.damage);

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
