namespace Cadenza
{
    public class HeavyAttackState : IState
    {
        public void Enter(Character character)
        {
            character.Animator.SetTrigger("HeavyAttack");

            // Heavy attack.
            character.comboM.ProcessCombo(AttkTypes.Heavy, out var reward);

            character.ManageAttackDirection();
            int flowDamage = character.HasFlowBuff(2) ? 1 : 0;
            float damageModifier = character.baseHeavyDamage / 2 * flowDamage * reward.Multiplier;

            //character.AttackArea.damage = (int)(character.baseHeavyDamage * damageModifier); // TEMP: should be a float
            //character.AttackArea.comboMove = reward.AttackEffect;
            //character.AttackArea.gameObject.SetActive(true);


            character.Schedule(
                character.attackDuration * 2f,
                () => character.ChangeState(character.walking));
        }

        public void Exit(Character character)
        {
            //character.AttackArea.gameObject.SetActive(false);
        }

        public void Update(Character character)
        {
        }

        public void FixedUpdate(Character character)
        {
        }
    }
}
