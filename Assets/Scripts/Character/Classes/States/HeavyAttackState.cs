namespace Cadenza
{
    public class HeavyAttackState : IState
    {
        public void Enter(Character character)
        {
            //Heavy Attack.
            character.AttackArea.StartHeavyAttack(character);

            character.Animator.SetTrigger("HeavyAttack");

            character.Schedule(
                character.attackDuration * 2f,
                () => character.ChangeState(character.walking));
        }

        public void Exit(Character character)
        {
            character.AttackArea.EndAttack();
        }

        public void Update(Character character)
        {
        }

        public void FixedUpdate(Character character)
        {
        }
    }
}
