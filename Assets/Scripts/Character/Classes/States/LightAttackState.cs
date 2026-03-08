namespace Cadenza
{
    public class LightAttackState : IState
    {
        public void Enter(Character character)
        {
            if (character.input.lightAttack == null)
                return;

            var score = character.input.lightAttack.Value;
            character.UpdateAccuracy(score);
            character.UpdateFlow(score);

            // Light attack.
            character.AttackArea.StartLightAttack(character);
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
