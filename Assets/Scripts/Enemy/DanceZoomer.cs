using UnityEngine;

namespace Cadenza
{
    public class DanceZoomer : Enemy
    {
        int curPhase = 0;
        int beatCount = 0;
        bool inZoom = false;
        bool inZoomSetup = false;
        float lerpTime = 0;
        float startPosition;
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
                case 3:
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
            this.curPhase = 1;
            //Moves enemy out of stage and freezes them in places
            this.transform.position = new Vector3(100,100,100);
            this.rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        private void Phase2()
        {
            this.curPhase = 2;
            //Places enemy in center of the stage will add better animation to this later
            this.transform.position = new Vector3(0, 5, 0);

        }

        private void Phase3()
        {
            this.curPhase = 3;
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
            //Resets enemy rigid body to allow movement but restrict rotation in the X and Z axis
            this.rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
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
            switch(this.curPhase)
            {
                case 1:
                    break;
                case 2:
                    this.Zoom();
                    break;
                case 3:
                    break;
            }
        }

        void Zoom()
        {
            if(!this.inZoom) 
            {
                int playerId = Random.Range(1, PlayerSystem.PlayerCount + 1);
                Player targetPlayer = PlayerSystem.Players[playerId];
                if(this.transform.position.x <= 0)
                {
                    this.TargetLocation = new Vector2(-10, targetPlayer.transform.position.z);
                }
                else
                {
                    this.TargetLocation = new Vector2(10, targetPlayer.transform.position.z);
                }
                this.startPosition = this.transform.position.x;
                this.inZoom = true;
                this.inZoomSetup = true;
            }
            else if(this.inZoomSetup)
            {
                Vector2 distance = new Vector2(this.transform.position.x, this.transform.position.z) - this.TargetLocation;
                if(distance.SqrMagnitude() < 2)
                {
                    this.inZoomSetup = false;
                    this.startPosition = this.TargetLocation.x;

                    this.TargetLocation.x *= -1;
                    this.lerpTime = 0;
                    this.attackArea.SetActive(true);
                }
                else
                {
                    this.transform.position = new Vector3(Mathf.Lerp(this.startPosition, this.TargetLocation.x, this.lerpTime), 
                                                        this.transform.position.y, this.transform.position.z);
                    this.lerpTime += Time.deltaTime;
                }
            }
            else
            {
                Vector2 distance = new Vector2(this.transform.position.x, this.transform.position.z) - this.TargetLocation;
                if(distance.SqrMagnitude() < 2)
                {
                    this.inZoom = false;
                    this.lerpTime = 0;
                    this.attackArea.SetActive(false);
                }
                else
                {
                    this.transform.position = new Vector3(Mathf.Lerp(this.startPosition, this.TargetLocation.x, this.lerpTime), 
                                                        this.transform.position.y, this.transform.position.z);
                    this.lerpTime += Time.deltaTime;
                }
            }

        }
    }
}
