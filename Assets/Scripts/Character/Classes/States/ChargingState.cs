namespace Cadenza
{
    public class ChargingState : IState
    {
        private Character chargingCharacter;

        public void Enter(Character character)
        {
            this.chargingCharacter = character;
            character.ChargeBeatsPassed = 0;

            BeatSystem.BeatPlayed += this.OnBeatPlayed;

            // Set visuals.
            character.Animator.SetBool("IsCharging", true);
            character.ChargeEffect.enabled = true;

            if (character.input.heavyAttack.HasValue)
                character.ChangeState(character.heavyAttack);
        }

        public void Exit(Character character)
        {
            BeatSystem.BeatPlayed -= this.OnBeatPlayed;
            character.Animator.SetBool("IsCharging", false);
            character.ChargeEffect.enabled = false;
        }

        public void Update(Character character)
        {
            if (character.input.heavyAttack.HasValue)
                character.ChangeState(character.heavyAttack);
        }

        public void FixedUpdate(Character character)
        {
            character.FlipSpriteFromVelocity(character.input.move);
        }

        private void OnBeatPlayed()
        {
            if (this.chargingCharacter != null)
                this.chargingCharacter.ChargeBeatsPassed++;
        }
    }
}
