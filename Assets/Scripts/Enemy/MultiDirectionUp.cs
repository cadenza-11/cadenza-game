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

        protected override void RangedAttack()
        {
            if (this.rangedAttackInterval <= 0)
            {
                base.RangedAttack();
                this.rangedAttackInterval = 1.5f;
            }
            else
            {
                this.rangedAttackInterval -= Time.deltaTime;
            }
        }

        protected override void IdleState()
        {
            this.rb.linearVelocity = Vector3.zero;
            this.curState = EnemyState.Run;
        }

        protected override void RangedState()
        {
            //Checks whether or not the enemy its attacks are aligned with the x or "z" axis
            //For the purposes of this code the "z" axis will be referred to as the y-axis (might change later not sure ;-;)
            if(this.horizontal && Math.Abs(this.startYPosition - this.transform.position.z) > 10)
            {
                this.TargetLocation.x = this.transform.position.x;
                this.curState = EnemyState.Run;
                return;
            }
            else if(!this.horizontal && Math.Abs(this.startXPosition - this.transform.position.x) > 10)
            {
                this.TargetLocation.y = this.transform.position.z;
                this.curState = EnemyState.Run;
                return;
            }

            Vector2 playerPos = this.FindNearestPlayerDist();
            if(this.horizontal)
            {
                this.TargetLocation.x = playerPos.x;
            }
            else
            {
                this.TargetLocation.y = playerPos.y;
            }
            //Above code follows the nearest player in either the x or y-axis.

            base.RunState();

            if(this.nearestPlayerDist < 5) //May want to change this value to an in-editor variable?
            {
                /*If a player is within a certain distance, have a 50% of doing a melee attack
                This attack will knock the player away from the enemy. The other 50% chance is to do a ranged attack */
                if(UnityEngine.Random.Range(1, 20) <= 10)
                {
                    Debug.Log("Does a melee attack");
                    this.MeleeAttack();
                }
                else
                {
                    Debug.Log("Does a ranged attack");
                    this.RangedAttack();
                }
            }
            else
            {
                //Otherwise just do a ranged attack like normal.
                this.RangedAttack();
            }
        }

        protected override void RunState()
        {
            base.RunState();
            if(!this.horizontal && !this.posDirection)
            {
                this.curAngle = (float)Math.PI;
            }
            else
            {
                this.curAngle = 0;
            }
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