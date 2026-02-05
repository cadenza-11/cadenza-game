using UnityEngine;
using System;

//Royce Ortega
namespace Cadenza
{
    public class MultiDirectionUp : Enemy
    {
        protected override void IdleState()
        {
            this.rb.linearVelocity = Vector3.zero;
            this.curState = EnemyState.Run;
        }

        protected override void RangedState()
        {
            
        }

        protected override void RunState()
        {
            
        }
    }
}