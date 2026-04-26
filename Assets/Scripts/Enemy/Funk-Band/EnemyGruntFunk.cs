using UnityEngine;
using System;

namespace Cadenza
{
    /* Some function just call the parent function rather than overriding anything. This is temporary and if I don't implement anything
    extra in them they will be removed */
    public class EnemyGruntFunk : Enemy
    {
        [SerializeField] private float moveTimer = -1;
        private bool continueMelee;
        [SerializeField] private int moveDir = -1;
        private EnemyState mainState;
        private float stateTimer;

        void Start()
        {
            EnemyManager.AddEnemy(this);
            this.Initialize();
        }

        public override void Initialize()
        {
            base.Initialize();
            this.moveTimer = 0;
            this.stateTimer = 0;
            this.RandomMainState();
        }

        private void RandomMainState()
        {
            switch (UnityEngine.Random.Range(0, 3))
            {
                case (0):
                    this.mainState = EnemyState.Chase;
                    break;

                case (1):
                    this.mainState = EnemyState.Assist;
                    break;

                case (2):
                    this.mainState = EnemyState.Zigzag;
                    break;
            }
        }

        public override void Awake()
        {
            this.isActionable = true;
            base.Awake();
        }
        protected override void RangedAttack()
        {

        }

        protected override void IdleState()
        {

            if (this.moveTimer <= 0)
            {
                this.moveDir = UnityEngine.Random.Range(1, 5);
                this.TargetLocation = this.FindNearestPlayerDist();
                this.moveTimer = UnityEngine.Random.Range(1, 6);
            }

            if (this.currentHealth <= 0)
            {
                this.curState = EnemyState.Dead;
            }
            else
            {
                this.curState = this.mainState;
            }
        }

        protected override void MeleeState()
        {
            if (this.moveTimer <= 0 && this.continueMelee)
            {
                this.moveTimer = UnityEngine.Random.Range(5, 10);
                this.continueMelee = false;
            }
            if (this.moveTimer <= 0 && !this.continueMelee)
            {
                this.curState = EnemyState.Idle;
            }
            this.MeleeAttack();
            this.moveTimer -= Time.deltaTime;

            if (this.currentHealth <= 0)
            {
                this.curState = EnemyState.Dead;
            }
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        private void Update()
        {
            this.stateTimer += Time.deltaTime;
            if(this.stateTimer >= 10f)
            {
                this.stateTimer = 0f;
                this.RandomMainState();
            }
        }

        protected override void CheckState()
        {
            if (!this.isActionable)
                return;

            switch (this.curState)
            {
                case EnemyState.Idle:
                    this.IdleState();
                    break;
                case EnemyState.Chase:
                    this.ChaseState();
                    break;
                case EnemyState.Melee:
                    this.MeleeState();
                    break;
                case EnemyState.Assist:
                    this.AssistState();
                    break;
                case EnemyState.Zigzag:
                    this.ZigzagState();
                    break;
                case EnemyState.Dead:
                    this.DeadState();
                    break;
            }
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
            if (toTarget.SqrMagnitude() < meleeDistance * meleeDistance)
            {
                this.curState = EnemyState.Melee;
            }

            if (this.currentHealth <= 0)
            {
                this.curState = EnemyState.Dead;
            }
        }

        void AssistState()
        {
            this.TargetLocation.x = this.follow.Character.transform.position.x;
            this.TargetLocation.y = this.follow.Character.transform.position.z;
            Vector2 toTarget = new Vector2(this.TargetLocation.x - this.transform.position.x,
                                            this.TargetLocation.y - this.transform.position.z);
            this.curAngle = (float)Math.Atan2(toTarget.y, toTarget.x);
            Vector3 moveDir = new Vector3(this.speed * (float)Math.Cos(this.curAngle), this.rb.linearVelocity.y, this.speed * (float)Math.Sin(this.curAngle));
            this.FindNearestPlayerDist();
            if(this.nearestPlayerDist >= 3f || this.nearestPlayerDist <= 1f)
            {
                this.rb.linearVelocity = moveDir;
            }
            else
            {
                this.rb.linearVelocity = Vector3.zero;
            }
            if (toTarget.SqrMagnitude() < meleeDistance * meleeDistance)
            {
                this.curState = EnemyState.Melee;
            }

            if (this.currentHealth <= 0)
            {
                this.curState = EnemyState.Dead;
            }
        }

        void ZigzagState()
        {
            this.TargetLocation.x = this.follow.Character.transform.position.x;
            this.TargetLocation.y = this.follow.Character.transform.position.z;
            Vector2 toTarget = new Vector2(this.TargetLocation.x - this.transform.position.x,
                                            this.TargetLocation.y - this.transform.position.z);
            this.curAngle = (float)Math.Atan2(toTarget.y, toTarget.x);
            Vector3 moveDir = new Vector3(this.speed * (float)Math.Cos(this.curAngle), this.rb.linearVelocity.y, this.speed * (float)Math.Sin(this.curAngle));
            if (this.moveTimer <= 0)
            {
                this.moveDir = UnityEngine.Random.Range(1, 6);
                this.TargetLocation = this.FindNearestPlayerDist();
                this.moveTimer = UnityEngine.Random.Range(1, 4);
            }
            switch (this.moveDir)
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
                case 5:
                    this.rb.linearVelocity = moveDir;
                    break;
            }
            this.moveTimer -= Time.deltaTime;

            if (toTarget.SqrMagnitude() < meleeDistance * meleeDistance)
            {
                this.curState = EnemyState.Melee;
            }

            if (this.currentHealth <= 0)
            {
                this.curState = EnemyState.Dead;
            }
        }

        protected override void DeadState()
        {
            base.DeadState();
        }
    }
}
