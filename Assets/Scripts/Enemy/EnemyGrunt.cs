using UnityEngine;
using System;

//Royce Ortega
namespace Cadenza
{
    /* Some function just call the parent function rather than overriding anything. This is temporary and if I don't implement anything
    extra in them they will be removed */
    public class EnemyGrunt : Enemy
    {
        [SerializeField] private float moveTimer = -1;
        private static int rotationNum = 0;
        private bool continueMelee;
        private int beatCount = 0;
        [SerializeField] private int moveDir = -1;
        

        public override void Initialize()
        {
            base.Initialize();
            this.moveTimer = 0;
        }
        
        public override void Awake()
        {
            this.isActionable = true;
            BeatSystem.BeatPlayed += this.onBeat;
            base.Awake();
        }

        protected override void OnDestroy()
        {
            BeatSystem.BeatPlayed -= this.onBeat;
            base.OnDestroy();
        }

        private void onBeat()
        {
            this.beatCount++;
        }
        protected override void RangedAttack()
        {
            
        }

        protected override void IdleState()
        {
            //Need to make better plan for movement. Right now Idle movement will be random
            if(this.moveTimer <= 0) 
            {
                this.moveDir = UnityEngine.Random.Range(1, 5);
                this.TargetLocation = this.FindNearestPlayerDist();
                this.moveTimer = UnityEngine.Random.Range(1, 6);
            }
            switch(this.moveDir)
                {
                    case 1:
                        this.rb.linearVelocity = new Vector3(this.speed, this.rb.linearVelocity.y, 0);
                        break;
                    case 2:
                        this.rb.linearVelocity = new Vector3(-1 * this.speed, this.rb.linearVelocity.y, 0);
                        break;
                    case 3:
                        this.rb.linearVelocity = new Vector3(0, this.rb.linearVelocity.y, this.speed);
                        break;
                    case 4:
                        this.rb.linearVelocity = new Vector3(0, this.rb.linearVelocity.y, -1 * this.speed);
                        break;
                }
            this.moveTimer -= Time.deltaTime;

            this.FindNearestPlayerDist();
            Debug.Log(this.nearestPlayerDist + ",  " + this.beatCount);

            if(this.nearestPlayerDist < 2 && this.beatCount > 4)
            {
                this.MeleeAttack();
                this.beatCount = 0;
            }

            if(UnityEngine.Random.Range(1, 1000) < 2)
            {
                this.moveTimer = 0;
                this.continueMelee = true;
                this.TargetLocation = this.FindNearestPlayerDist();
                EnemyManager.GroupAttack();
            }

            if(this.currentHealth <= 0)
            {
                this.curState = EnemyState.Dead;
            }
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

            if(this.currentHealth <= 0)
            {
                this.curState = EnemyState.Dead;
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

            if(this.currentHealth <= 0)
            {
                this.curState = EnemyState.Dead;
            }
        }

        public void GroupAttack(Vector2 location)
        {
            //Sets the enemies targetLocation as the location plus Cos and Sin values. Does this so that the enemies will gather
            //In a circle around a player
            Vector2 target = new Vector2(location.x + 1 * Mathf.Cos(rotationNum * Mathf.PI/6), 
                                        location.y + 1 * Mathf.Sin(rotationNum * Mathf.PI/6));
            rotationNum++;
            rotationNum %= 12;
            this.TargetLocation = target;
            this.curState = EnemyState.Chase;
        }

        protected override void DeadState()
        {
            if(!EnemyManager.CheckGrunts(this))
            {
                Debug.Log("Requests next phase");
                //Sets the phase Index to 1
                AudioSystem.SetParameter("MusicState", 1);
            }
            base.DeadState();
        }
    }
}
