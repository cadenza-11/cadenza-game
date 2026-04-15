using UnityEngine;

namespace Cadenza
{
    public class ParryState : IState
    {
        public void Enter(Character character)
        {
            if (!character.input.parry.HasValue)
            {
                character.ChangeState(character.walking);
                return;
            }

            var score = character.input.parry.Value;
            float totalDuration = Mathf.Max(0.01f, character.attackDuration);
            float activeDuration = totalDuration * 0.5f;

            character.UpdateAccuracy(score);
            character.UpdateFlow(score);
            character.ActivateParryWindow(totalDuration);

            // Animate.
            character.Animator.SetBool("IsMove", false);
            character.Animator.SetTrigger("Parry");

            // Stop velocity.
            character.Rigidbody.linearVelocity = new Vector3(0f, character.Rigidbody.linearVelocity.y, 0f);

            character.Schedule(totalDuration, () =>
            {
                if (character.CurrentState == this)
                    character.ChangeState(character.walking);
            });
        }

        public void Exit(Character character)
        {
            character.ClearParryWindow();
        }

        public void Update(Character character)
        {
        }

        public void FixedUpdate(Character character)
        {
            character.Rigidbody.linearVelocity = new Vector3(
                0f,
                character.Rigidbody.linearVelocity.y,
                0f
            );
        }
    }
}
