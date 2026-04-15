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
        private static int rotationNum = 0;
        private bool continueMelee;
        

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

            if(UnityEngine.Random.Range(1, 1000) < 2)
            {
                this.moveTimer = 0;
                this.continueMelee = true;
                this.FindNearestPlayerDist();
                EnemyManager.GroupAttack();
            }
        }

        protected override void RangedState()
        {
            
        }

        protected override void RunState()
        {
            
        }

        protected override void MeleeState()
        {
            if(this.moveTimer <= 0 && this.continueMelee)
            {
                this.moveTimer = UnityEngine.Random.Range(5, 10);
                this.continueMelee = false;
            }
            if(this.moveTimer <= 0 && !this.continueMelee)
            {
                this.curState = EnemyState.Idle;
            }
            this.MeleeAttack();
            this.moveTimer -= Time.deltaTime;
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
            this.TargetLocation.x = this.follow.Character.transform.position.x;
            this.TargetLocation.y = this.follow.Character.transform.position.z;
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
            //Debug.Log("Grunt Starting Group Attack   " + rotationNum);
            //Debug.Log("X: " + location.x + "  Y: " + location.y);
            //Sets the enemies targetLocation as the location plus Cos and Sin values. Does this so that the enemies will gather
            //In a circle around a player
            Vector2 target = new Vector2(location.x + 1 * Mathf.Cos(rotationNum * Mathf.PI/6), 
                                        location.y + 1 * Mathf.Sin(rotationNum * Mathf.PI/6));
            rotationNum++;
            rotationNum %= 12;
            this.TargetLocation = target;
            this.curState = EnemyState.Chase;
        }
    }
}
