namespace Cadenza
{
    public class FaintedState : IState
    {
        public void Enter(Character character)
        {
            character.isFainted = true;
            character.Animator.SetBool("IsFainted", true);

            // TEMP: Revive self after fainting.
            character.Schedule(2.0f, () => character.ChangeState(character.walking));
        }

        public void Exit(Character character)
        {
            character.isFainted = false;
            character.Animator.SetBool("IsFainted", false);

            character.SetHealth(character.maxHealth);
        }

        public void FixedUpdate(Character character)
        {
        }

        public void Update(Character character)
        {
        }
    }
}
