using UnityEngine;

namespace Cadenza
{
    public class DanceZoomer : Enemy
    {
        [SerializeField] int curPhase = 0;
        int beatCount = 0;
        bool inZoom = false;
        bool inZoomSetup = false;
        float lerpTime = 0;
        float pillarLerpTime = 0;
        Vector2 startPosition;
        [SerializeField] GameObject[] pillars = new GameObject[11];
        bool[] isPillarRising = {false, false, false, false, false, false, false, false, false, false, false};
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

        protected override void OnDestroy()
        {
            GameManager.PhaseEntered -= this.OnPhaseEntered;
            GameManager.PhaseExited -= this.OnPhaseExit;
            BeatSystem.BeatPlayed -= this.onBeat;
            base.OnDestroy();
        }

        private void onBeat()
        {
            this.beatCount++;
        }

        private void OnPhaseEntered(Phase phase)
        {
            Debug.Log(phase.Index);
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
            //this.transform.position = new Vector3(100,100,100);
            this.rb.constraints = RigidbodyConstraints.FreezeAll;
            this.rb.linearVelocity = Vector3.zero;
            EnemyManager.GruntPhase();
        }

        private void Phase2()
        {
            Debug.Log("Goes into second phase");
            //Resets enemy rigid body to allow movement but restrict rotation in the X and Z axis
            this.rb.constraints = RigidbodyConstraints.FreezeRotation;
            //Places enemy in center of the stage will add better animation to this later
            this.transform.position = new Vector3(0.5f, 5, 6);
            this.rb.linearVelocity = Vector3.zero;
            this.curPhase = 2;

        }

        private void Phase3()
        {
            Debug.Log("Goes into third phase");
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
            Debug.Log("Exits first phase");
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
                int numPlayers = 0;
                foreach(var player in PlayerSystem.Players)
                {
                    numPlayers++;
                }
                int playerId = Random.Range(1, numPlayers + 1);
                Debug.Log("" + playerId + ",  " + numPlayers + 1);
                Player targetPlayer = PlayerSystem.Players[playerId - 1];
                Debug.Log(targetPlayer.Character.transform.position.x + ",  " + targetPlayer.Character.transform.position.z);
                if(this.transform.position.x <= 0)
                {
                    this.TargetLocation = new Vector2(-7, targetPlayer.Character.transform.position.z);
                }
                else
                {
                    this.TargetLocation = new Vector2(7, targetPlayer.Character.transform.position.z);
                }
                this.startPosition.x = this.transform.position.x;
                this.startPosition.y = this.transform.position.z;
                this.inZoom = true;
                this.inZoomSetup = true;
                this.ChoosePillar();
            }
            else if(this.inZoomSetup)
            {
                for(int i = 0; i < 11; i++)
                {
                    if(this.isPillarRising[i])
                    {
                        this.pillars[i].transform.position = new Vector3(this.pillars[i].transform.position.x, 
                                Mathf.Lerp(-1.5f, 0.1f, this.pillarLerpTime/2.0f), this.pillars[i].transform.position.z);
                    }
                    else
                    {
                        this.pillars[i].transform.position = new Vector3(this.pillars[i].transform.position.x,
                                Mathf.Lerp(0.1f, -1.5f, this.pillarLerpTime/2.0f), this.pillars[i].transform.position.z);
                    }
                }
                this.pillarLerpTime += Time.deltaTime;
                Vector2 distance = new Vector2(this.transform.position.x, this.transform.position.z) - this.TargetLocation;
                if(distance.SqrMagnitude() < 2)
                {
                    this.inZoomSetup = false;
                    this.startPosition.x = this.TargetLocation.x;
                    this.startPosition.y = this.TargetLocation.y;

                    this.TargetLocation.x *= -1;
                    this.lerpTime = 0;
                    this.pillarLerpTime = 0;
                    this.attackArea.SetActive(true);
                }
                else
                {
                    this.transform.position = new Vector3(Mathf.Lerp(this.startPosition.x, this.TargetLocation.x, this.lerpTime/1.0f), 
                                                        this.transform.position.y, Mathf.Lerp(this.startPosition.y, this.TargetLocation.y, this.lerpTime/1.0f));
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

                    for(int i = 0; i < 11; i++)
                    {
                        this.isPillarRising[i] = false;
                    }
                }
                else
                {
                    this.transform.position = new Vector3(Mathf.Lerp(this.startPosition.x, this.TargetLocation.x, this.lerpTime/1.0f), 
                                                        this.transform.position.y, Mathf.Lerp(this.startPosition.y, this.TargetLocation.y, this.lerpTime/1.0f));
                    this.lerpTime += Time.deltaTime;
                }
            }

        }

        void ChoosePillar()
        {
            int numPillars = Random.Range(1, 12);
            for(int i = 0; i < numPillars; i++)
            {
                int index = Random.Range(1, 12);
                if(!this.isPillarRising[index - 1])
                {
                    this.isPillarRising[index - 1] = true;
                }
                else if(Random.Range(1, 3) < 2)
                {
                    int j = 0;
                    while(this.isPillarRising[j])
                    {
                        j++;
                    }
                    this.isPillarRising[j] = true;
                }
                else
                {
                    int j = 10;
                    while(this.isPillarRising[j])
                    {
                        j--;
                    }
                    this.isPillarRising[j] = true;
                }
            }
        }
    }
}
