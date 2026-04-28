using UnityEngine;

namespace Cadenza
{
    public class BlockState : IState
    {
        public void Enter(Character character)
        {
            if (!character.IsBlockHeld() && !character.input.blockPressed.HasValue)
            {
                character.ChangeState(character.walking);
                return;
            }

            if (character.input.blockPressed.HasValue)
                character.ProcessBlockTiming(character.input.blockPressed.Value);

            character.BeginBlock();

            // Animate.
            character.Animator.SetBool("IsMove", false);
            character.Animator.SetBool("IsBlocking", true);
        }

        public void Exit(Character character)
        {
            character.Animator.SetBool("IsBlocking", false);
            character.EndBlock();
        }

        public void Update(Character character)
        {
            if (character.input.blockPressed.HasValue)
                character.ProcessBlockTiming(character.input.blockPressed.Value);

            if (character.input.blockReleased.HasValue)
                character.ProcessBlockTiming(character.input.blockReleased.Value);

            if (!character.IsBlockHeld())
                character.ChangeState(character.walking);
        }

        public void FixedUpdate(Character character)
        {
        }
    }
}
