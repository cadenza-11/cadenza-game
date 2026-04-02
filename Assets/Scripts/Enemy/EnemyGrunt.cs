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

        public void GroupAttack(Vector2 location)
        {
            this.TargetLocation = location;
        }
    }
}
