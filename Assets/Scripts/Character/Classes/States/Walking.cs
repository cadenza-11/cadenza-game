namespace Cadenza
{
    public class WalkingState : IState
    {
        public void Enter(Character character)
        {
        }

        public void Exit(Character character)
        {
            character.StopGroundMovement();
        }

        public void Update(Character character)
        {
            // Transition using priority.
            if (character.input.wantTeam)
                character.StartTeamAttack();

            else if (character.input.lightAttack.HasValue)
                character.ChangeState(character.lightAttack);

            else if (character.input.charge.HasValue)
                character.ChangeState(character.charging);

            else if (character.input.heavyAttack.HasValue)
                character.ChangeState(character.heavyAttack);

            else if (character.input.blockPressed.HasValue || character.IsBlockHeld())
                character.ChangeState(character.block);
        }

        public void FixedUpdate(Character character)
        {
            character.UpdateGroundMovement();
        }
    }
}
