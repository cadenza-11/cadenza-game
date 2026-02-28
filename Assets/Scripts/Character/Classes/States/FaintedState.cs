namespace Cadenza
{
    public class FaintedState : IState
    {
        public void Enter(Character character)
        {
            character.isFainted = true;
            character.Animator.SetBool("IsFainted", true);
            character.RevivalMeter.Show();
        }

        public void Exit(Character character)
        {
            character.isFainted = false;
            character.Animator.SetBool("IsFainted", false);
            character.RevivalMeter.Hide();

            character.SetHealth(character.maxHealth/2);
        }

        public void FixedUpdate(Character character)
        {
        }

        public void Update(Character character)
        {
        }
    }
}
