using UnityEngine;

namespace Cadenza
{
    public class DanceZoomer : Enemy
    {
        [SerializeField] int curPhase = 0;
        int beatCount = 0;
        bool inZoom = false;
        bool inZoomSetup = false;
        bool phase3Transition = true;
        bool hasCollided = false;
        float lerpTime = 0;
        float pillarLerpTime = 0;
        [SerializeField] float downedTime = 0;
        Vector2 startPosition;
        [SerializeField] GameObject[] pillars = new GameObject[11];
        [SerializeField] ZoomerAttack attackBox;
        
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
            Debug.Log("Goes into OnDestroy");
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
            this.rb.constraints = RigidbodyConstraints.None;
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
            this.rb.constraints = RigidbodyConstraints.FreezeRotation;
            this.rb.linearVelocity = Vector3.zero;
            this.curPhase++;
        }

        private void Phase3()
        {
            Debug.Log("Enters phase 3");
            this.phase3Transition = true;
            this.pillarLerpTime = 0;
            this.attackBox.ResetNumCollisions();
            this.attackBox.SetActive(false);
            this.rb.constraints = RigidbodyConstraints.FreezeAll;
            this.curPhase++;
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
            if(this.currentHealth <= 0)
            {
                this.DeadState();
            }
            switch(this.curPhase)
            {
                case 1:
                    break;
                case 2:
                    this.Zoom();
                    break;
                case 3:
                    if(this.phase3Transition)
                    {
                        this.ThirdPhaseTransition();
                    }
                    if(this.downedTime >= 10.0f)
                    {
                        Debug.Log("Trying to get out of downed phase");
                        AudioSystem.SetParameter("MusicState", this.curPhase);
                        this.downedTime = 0;
                    }
                    this.downedTime += Time.deltaTime;
                    break;
                case 4:
                    break;
                case 5:
                    this.Zoom();
                    break;
                case 6:
                    if(this.phase3Transition)
                    {
                        this.ThirdPhaseTransition();
                    }
                    //Wait until players kill enemy
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
                Player targetPlayer = PlayerSystem.Players[playerId - 1];
                float zLocation = targetPlayer.Character.transform.position.z;
                if(zLocation > 10.5)
                {
                    zLocation = 10.4f;
                }
                else if(zLocation < 2.5)
                {
                    zLocation = 2.6f;
                }
                if(this.transform.position.x <= 0)
                {
                    this.TargetLocation = new Vector2(-7, zLocation);
                }
                else
                {
                    this.TargetLocation = new Vector2(7, zLocation);
                }
                this.startPosition.x = this.transform.position.x;
                this.startPosition.y = this.transform.position.z;
                this.inZoom = true;
                this.inZoomSetup = true;
                this.ChoosePillar();
                //this.anim.SetTrigger("zoomer_charge");
            }
            else if(this.inZoomSetup)
            {
                for(int i = 0; i < 11; i++)
                {
                    if(this.isPillarRising[i])
                    {
                        this.pillars[i].transform.position = new Vector3(this.pillars[i].transform.position.x, 
                                Mathf.Lerp(this.pillars[i].transform.position.y, 0.1f, this.pillarLerpTime/2.0f), 
                                this.pillars[i].transform.position.z);
                    }
                    else
                    {
                        this.pillars[i].transform.position = new Vector3(this.pillars[i].transform.position.x,
                                Mathf.Lerp(this.pillars[i].transform.position.y, -2.0f, this.pillarLerpTime/2.0f), 
                                this.pillars[i].transform.position.z);
                    }
                }
                this.pillarLerpTime += Time.deltaTime;
                Vector2 distance = new Vector2(this.transform.position.x, this.transform.position.z) - this.TargetLocation;
                if(distance.SqrMagnitude() < 2 && this.pillarLerpTime >= 2.0f)
                {
                    this.inZoomSetup = false;
                    this.startPosition.x = this.TargetLocation.x;
                    this.startPosition.y = this.TargetLocation.y;

                    this.TargetLocation.x *= -1;
                    this.lerpTime = 0;
                    this.pillarLerpTime = 0;
                    this.attackBox.SetActive(true);
                    this.anim.SetBool("IsMove", true);
                    Debug.Log("Is in Charging?: " + this.anim.GetCurrentAnimatorStateInfo(0).IsName("Charging"));
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
                Debug.Log("Is in Charging?: " + this.anim.GetCurrentAnimatorStateInfo(0).IsName("Charging"));
                Vector2 distance = new Vector2(this.transform.position.x, this.transform.position.z) - this.TargetLocation;
                if(this.hasCollided)
                {
                    this.Collision();
                }
                else if(this.attackBox.GetHasCollided())
                {
                     this.anim.SetBool("IsMove", false);
                    Debug.Log("Collided");
                    this.hasCollided = true;
                    this.pillarLerpTime = 0;
                    this.attackBox.SetActive(false);
                    for(int i = 0; i < 11; i++)
                    {
                        this.isPillarRising[i] = false;
                    }
                    //this.anim.SetTrigger("zoomer_land");
                    if(this.attackBox.GetNumCollisions() >= 3)
                    {
                        Debug.Log("Tries to enter downed state");
                        AudioSystem.SetParameter("MusicState", this.curPhase);
                    }

                    //Moves enemy a bit even after it has collided with a pillar
                    this.transform.position = new Vector3(Mathf.Lerp(this.startPosition.x, this.TargetLocation.x, this.lerpTime/1.0f), 
                                                        this.transform.position.y, Mathf.Lerp(this.startPosition.y, this.TargetLocation.y, this.lerpTime/1.0f));
                    this.lerpTime = 0;
                }
                else if(distance.SqrMagnitude() < 2)
                {
                    this.anim.SetBool("IsMove", false);
                    this.inZoom = false;
                    this.lerpTime = 0;
                    this.attackBox.SetActive(false);
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

        void Collision()
        {
            if(this.pillarLerpTime <= 2)
            {
                for(int i = 0; i < 11; i++)
                {
                    this.pillars[i].transform.position = new Vector3(this.pillars[i].transform.position.x,
                                Mathf.Lerp(this.pillars[i].transform.position.y, -2.0f, this.pillarLerpTime/2.0f), 
                                this.pillars[i].transform.position.z);
                }
                this.pillarLerpTime += Time.deltaTime;
            }
            if(this.downedTime > 3)
            {
                this.inZoom = false;
                this.hasCollided = false;
                this.attackBox.ResetHasCollided();
                this.pillarLerpTime = 0;
                this.downedTime = 0;
            }
            this.downedTime += Time.deltaTime;
        }

        void ThirdPhaseTransition()
        {
            this.pillarLerpTime += Time.deltaTime;
            for(int i = 0; i < 11; i++)
            {
                this.pillars[i].transform.position = new Vector3(this.pillars[i].transform.position.x,
                                Mathf.Lerp(this.pillars[i].transform.position.y, -2.0f, this.pillarLerpTime/2.0f), 
                                this.pillars[i].transform.position.z);
            }

            if(this.pillarLerpTime >= 2.0f)
            {
                this.phase3Transition = false;
                this.pillarLerpTime = 0;
            }
        }
    }
}
