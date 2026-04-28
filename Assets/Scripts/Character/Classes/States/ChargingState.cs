namespace Cadenza
{
    public class ChargingState : IState
    {
        private Character chargingCharacter;
        private int pendingHeldChargeBeat = int.MinValue;

        public void Enter(Character character)
        {
            this.chargingCharacter = character;
            this.pendingHeldChargeBeat = int.MinValue;
            character.ChargeBeatsPassed = 0;

            BeatSystem.BeatPlayed += this.OnBeatPlayed;

            if (!character.input.charge.HasValue)
            {
                character.ChangeState(character.walking);
                return;
            }

            var score = character.input.charge.Value;
            character.UpdateAccuracy(score);
            character.UpdateFlow(score);
            character.comboM.ProcessCombo(AttkTypes.Charge, score, out _);
            character.comboM.SetChargeHeld(true);

            if (score.Class == ScoreClass.Bad)
                character.PlayFailSound();
            else
                character.PlayChargeSound(1);

            // Set visuals.
            character.Animator.SetBool("IsCharging", true);
            character.ChargeEffect.enabled = true;

            if (character.input.heavyAttack.HasValue)
                character.ChangeState(character.heavyAttack);
        }

        public void Exit(Character character)
        {
            BeatSystem.BeatPlayed -= this.OnBeatPlayed;
            this.pendingHeldChargeBeat = int.MinValue;
            character.comboM.SetChargeHeld(false);
            character.ClearChargeTracking();
            character.Animator.SetBool("IsCharging", false);
            character.ChargeEffect.enabled = false;
        }

        public void Update(Character character)
        {
            if (character.input.heavyAttack.HasValue)
            {
                character.ChangeState(character.heavyAttack);
                return;
            }

            this.ProcessPendingHeldCharge(character);
        }

        public void FixedUpdate(Character character)
        {
            character.FlipSpriteFromVelocity(character.input.move);
        }

        private void OnBeatPlayed()
        {
            if (this.chargingCharacter != null)
                this.pendingHeldChargeBeat = BeatSystem.GetClosestBeat(BeatSystem.CurrentTrackTime);
        }

        private void ProcessPendingHeldCharge(Character character)
        {
            if (this.pendingHeldChargeBeat == int.MinValue || BeatSystem.SecondsPerBeat <= 0)
                return;

            double heldChargeTime = this.pendingHeldChargeBeat * BeatSystem.SecondsPerBeat;
            double graceSeconds = ScoreSystem.IndividualThresholds.okScoreMs / 1000d;
            if (BeatSystem.CurrentTrackTime < heldChargeTime + graceSeconds)
                return;

            character.ProcessHeldChargeBeat(this.pendingHeldChargeBeat);
            this.pendingHeldChargeBeat = int.MinValue;
        }
    }
}
