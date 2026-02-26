using UnityEngine;

namespace Cadenza
{
    public class HitStunState : IState
    {
        private float duration = 0.2f;

        public HitStunState WithDuration(float seconds)
        {
            this.duration = Mathf.Max(0.01f, seconds);
            return this;
        }

        public void Enter(Character character)
        {
            character.Animator.SetTrigger("IsHit");
            character.Schedule(this.duration, () =>
            {
                if (!character.isFainted)
                    character.ChangeState(character.walking);
            });
        }

        public void Exit(Character character)
        {
        }

        public void Update(Character character)
        {
        }

        public void FixedUpdate(Character character)
        {
        }
    }
}
