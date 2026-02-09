namespace Cadenza
{
    public class LightAttackState : IState
    {
        public void Enter(Character character)
        {
            // Light attack.
            character.comboM.ProcessCombo(AttkTypes.Light, out var reward);
            character.ManageAttackDirection();

            int flowDamage = character.HasFlowBuff(2) ? 1 : 0;
            float damageModifier = character.baseLightDamage / 2 * flowDamage * reward.Multiplier;

            character.AttackArea.damage = (int)(character.baseLightDamage * damageModifier); // TEMP: should be flaot.
            character.AttackArea.comboMove = reward.AttackEffect;
            character.AttackArea.gameObject.SetActive(true);

            character.Animator.SetTrigger("LightAttack");

            character.Schedule(
                character.attackDuration,
                () => character.ChangeState(character.walking));
        }

        public void Exit(Character character)
        {
            character.AttackArea.gameObject.SetActive(false);
        }

        public void Update(Character character)
        {
        }

        public void FixedUpdate(Character character)
        {
        }
    }
}
