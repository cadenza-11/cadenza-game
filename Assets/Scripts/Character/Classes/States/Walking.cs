using UnityEngine;

namespace Cadenza
{
    public class WalkingState : IState
    {
        public void Enter(Character character)
        {
        }

        public void Exit(Character character)
        {
            character.Rigidbody.linearVelocity = Vector3.zero;
            character.Animator.SetBool("IsMove", false);
        }

        public void Update(Character character)
        {
            // Transition using priority.
            if (character.input.wantTeam)
                character.StartTeamAttack();

            else if (character.input.lightAttack.HasValue)
                character.ChangeState(character.lightAttack);

            else if (character.input.heavyAttack.HasValue)
                character.ChangeState(character.heavyAttack);
        }

        public void FixedUpdate(Character character)
        {
            // Walk.
            int flowSpeed = character.HasFlowBuff(0) ? 1 : 0;
            float speedModifier = character.speed + (character.speed * 0.25f * flowSpeed);

            Vector3 velocity = new(
                character.input.move.x * speedModifier,
                character.Rigidbody.linearVelocity.y,
                character.input.move.y * speedModifier
            );

            character.Rigidbody.linearVelocity = velocity;

            // Visual updates.
            bool moving = Mathf.Abs(velocity.x) > 0.001f || Mathf.Abs(velocity.z) > 0.001f;
            character.Animator.SetBool("IsMove", moving);

            if (Mathf.Abs(velocity.x) > 0.001f)
                character.FlipSpriteFromVelocity(velocity);
        }
    }
}
