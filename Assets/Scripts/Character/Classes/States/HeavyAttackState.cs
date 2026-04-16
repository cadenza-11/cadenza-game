namespace Cadenza
{
    public class HeavyAttackState : IState
    {
        public void Enter(Character character)
        {
            if (character.input.heavyAttack == null)
                return;

            if (character.input.heavyAttack.Value.Class == ScoreClass.Bad)
            {
                this.FailAttack(character);
            }
            else
            {
                this.DoHeavyAttack(character);
            }
        }

        public void FailAttack(Character character)
        {
            character.ChargeBeatsPassed = 0;
            character.Animator.SetTrigger("Fail");
            character.Schedule(
                character.attackDuration * 2f,
                () => character.ChangeState(character.walking));
        }

        public void DoHeavyAttack(Character character)
        {
            var score = character.input.heavyAttack.Value;
            character.UpdateAccuracy(score);
            character.UpdateFlow(score);

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
