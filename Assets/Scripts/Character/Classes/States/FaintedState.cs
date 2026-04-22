namespace Cadenza
{
    public class FaintedState : IState
    {
        public void Enter(Character character)
        {
            character.isFainted = true;
            character.StopGroundMovement();
            character.Animator.SetBool("IsFainted", true);
            character.RevivalMeter.Show();
        }

        public void Exit(Character character)
        {
            character.isFainted = false;
            character.Animator.SetBool("IsFainted", false);
            character.RevivalMeter.Hide();

            character.SetHealth(character.MaxHealth / 2);
        }

        public void FixedUpdate(Character character)
        {
            character.StopGroundMovement();
        }

        public void Update(Character character)
        {
            // Update revive.
            if (character.input.lightAttack.HasValue)
                character.UpdateRevive(character.input.lightAttack.Value);
            else if (character.input.heavyAttack.HasValue)
                character.UpdateRevive(character.input.heavyAttack.Value);
        }
    }
}
