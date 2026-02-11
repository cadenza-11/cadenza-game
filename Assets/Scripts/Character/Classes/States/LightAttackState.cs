namespace Cadenza
{
    public class LightAttackState : IState
    {
        public void Enter(Character character)
        {
            // Light attack.
            character.AttackArea.StartAttack(character);

            character.Animator.SetTrigger("LightAttack");

            character.Schedule(
                character.attackDuration,
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
