using UnityEngine;
using System;

//Royce Ortega
namespace Cadenza
{
    /* Some function just call the parent function rather than overriding anything. This is temporary and if I don't implement anything
    extra in them they will be removed */
    public class EnemyGrunt : Enemy
    {
        private float moveTimer;
        static int rotationNum = 0;

        public override void Initialize()
        {
            base.Initialize();
            this.moveTimer = 0;
        }

        protected override void RangedAttack()
        {
            
        }

        protected override void IdleState()
        {
            //Need to make better plan for movement. Right now Idle movement will be random
            if(this.moveTimer == 0) 
            {
                int moveDir = UnityEngine.Random.Range(1, 5);
                switch(moveDir)
                {
                    case 1:
                        this.rb.linearVelocity = new Vector3(1, 0, 0);
                        break;
                    case 2:
                        this.rb.linearVelocity = new Vector3(-1, 0, 0);
                        break;
                    case 3:
                        this.rb.linearVelocity = new Vector3(0, 0, 1);
                        break;
                    case 4:
                        this.rb.linearVelocity = new Vector3(0, 0, -1);
                        break;
                }
                this.TargetLocation = base.FindNearestPlayerDist();

                this.moveTimer = UnityEngine.Random.Range(1, 6);
            }
            this.moveTimer -= Time.deltaTime;
        }

        protected override void RangedState()
        {
            
        }

        protected override void RunState()
        {
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        protected override void CheckState()
        {
            base.CheckState();
        }

        protected override void ChaseState()
        {
            Vector2 toTarget = new Vector2(this.TargetLocation.x - this.transform.position.x, 
                                            this.TargetLocation.y - this.transform.position.z);
            this.curAngle = (float)Math.Atan2(toTarget.y, toTarget.x);
            Vector3 moveDir = new Vector3(this.speed * (float)Math.Cos(this.curAngle), this.rb.linearVelocity.y, this.speed * (float)Math.Sin(this.curAngle));
            this.rb.linearVelocity = moveDir;

            if(toTarget.SqrMagnitude() < meleeDistance * meleeDistance)
            {
                this.curState = EnemyState.Melee;
            }
        }

        public void GroupAttack(Vector2 location)
        {
            //Sets the enemies targetLocation as the location plus Cos and Sin values. Does this so that the enemies will gather
            //In a circle around a player
            Vector2 target = new Vector2(location.x + 5 * Mathf.Cos(rotationNum * Mathf.PI/6), 
                                        location.y + 5 * Mathf.Sin(rotationNum * Mathf.PI/6));
            rotationNum++;
            rotationNum %= 12;
            this.TargetLocation = target;
            this.curState = EnemyState.Chase;
        }
    }
}
