using UnityEngine;

namespace Cadenza
{
    public class DanceShooter : Enemy
    {
        [SerializeField] int curPhase = 0;
        int beatCount = 0;
        float lerpTime = 0;
        [SerializeField] new DanceShooterProjectile projectile;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            EnemyManager.AddEnemy(this);
        }

        public override void Initialize()
        {
            GameManager.PhaseEntered += this.OnPhaseEntered;
            GameManager.PhaseExited += this.OnPhaseExit;
            BeatSystem.BeatPlayed += this.onBeat;
        }

        private void onBeat()
        {
            this.beatCount++;
        }

        private void OnPhaseEntered(Phase phase)
        {
            switch(phase.Index)
            {
                case 0: //Grunt onslaught this enemy does nothing
                    this.Phase1();
                    break;
                case 1: //Zooms across stage, hitting players
                    this.Phase2();
                    break;
                case 2: //Enemy Downed
                    this.Phase3();
                    break;
                case 3: //Grunt onslaught again
                    this.Phase1();
                    break;
                case 4:
                    this.Phase2();
                    break;
                case 5:
                    this.Phase3();
                    break;
            }
        }

        private void Phase1()
        {
            this.curPhase++;
            //Moves enemy out of stage and freezes them in places
            this.transform.position = new Vector3(100,100,100);
            this.rb.constraints = RigidbodyConstraints.FreezeAll;
            this.rb.linearVelocity = Vector3.zero;
            EnemyManager.GruntPhase();
        }

        private void Phase2()
        {
            //Resets enemy rigid body to allow movement but restrict rotation in the X and Z axis
            //Places enemy in center of the stage will add better animation to this later
            this.transform.position = new Vector3(0.5f, 5, 6);
            this.rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            this.rb.linearVelocity = Vector3.zero;
            this.curPhase++;
        }

        private void Phase3()
        {
        }

        private void OnPhaseExit(Phase phase)
        {
            switch(phase.Index)
            {
                case 0: //Grunt onslaught
                    this.ExitPhase1();
                    break;
                case 1: //Zooms across stage
                    this.ExitPhase2();
                    break;
                case 2: //Enemy Downed
                    this.ExitPhase3();
                    break;
            }
        }

        private void ExitPhase1()
        {
        }

        private void ExitPhase2()
        {
            
        }

        private void ExitPhase3()
        {
            
        }

        // Update is called once per frame
        protected override void FixedUpdate()
        {
            switch(this.curPhase % 3)
            {
                case 0:
                    break;
                default:
                    break;
            }
        }
    }
}
