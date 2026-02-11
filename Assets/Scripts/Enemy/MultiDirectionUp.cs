using UnityEngine;
using System;
using System.Security;
using UnityEditor.ShaderKeywordFilter;

//Royce Ortega
namespace Cadenza
{
    /* Some function just call the parent function rather than overriding anything. This is temporary and if I don't implement anything
    extra in them they will be removed */
    public class MultiDirectionUp : Enemy
    {
        [SerializeField] float startXPosition; 
        [SerializeField] float startYPosition; //Both Positions give the position the enemy should start at.
        [SerializeField] bool horizontal; /* Gives whether or not the enemy should be aligned with a vertical or horizontal axis. 
                                             (moves left/right or up/down) */
        [SerializeField] bool posDirection; //If true the enemy is Up/Right, if false the enemy is Down/Left

        protected override void Start()
        {
            base.Start();
            this.TargetLocation.x = this.startXPosition;
            this.TargetLocation.y = this.startYPosition;
        }

        protected override void IdleState()
        {
            this.rb.linearVelocity = Vector3.zero;
            this.curState = EnemyState.Run;
        }

        protected override void RangedState()
        {
            //Checks whether or not the enemy is aligned with its given x or y axis
            if(this.horizontal && Math.Abs(this.startXPosition - this.transform.position.x) > 10)
            {
                this.TargetLocation.y = this.transform.position.y;
                this.curState = EnemyState.Run;
            }
            else if(!this.horizontal && Math.Abs(this.startYPosition - this.transform.position.y) > 10)
            {
                this.TargetLocation.x = this.transform.position.x;
                this.curState = EnemyState.Run;
            }

            this.RangedAttack();
        }

        protected override void RunState()
        {
            base.RunState();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        protected override void CheckState()
        {
            base.CheckState();
        }
    }
}