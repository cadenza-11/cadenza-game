using UnityEngine;
using System;

namespace Cadenza
{
    public class DanceShooter : Enemy
    {
        [SerializeField] int curPhase = 0;
        int beatCount = 0;
        float lerpTime = 0;
        bool enterPhase1 = false;
        bool enterPhase3 = false;
        float phaseTime = 0;
        [SerializeField] float downedTime = 0;
        [SerializeField] int beatsPerAttack;
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
            this.enterPhase1 = true;
            this.phaseTime = 0;
            this.rb.linearVelocity = Vector3.zero;
        }

        private void Phase2()
        {
            //Entering the second phase does nothing for this enemy
            this.curPhase++;
            this.phaseTime = 0;
        }

        private void Phase3()
        {
            this.curPhase++;
            this.enterPhase3 = true;
            this.downedTime = 0;
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
                    if(this.phaseTime >= 30.0f)
                    {
                        AudioSystem.SetParameter("MusicState", this.curPhase);
                    }
                    if(this.enterPhase1)
                    {
                        this.Phase1Setup();
                    }
                    else if(this.beatCount % this.beatsPerAttack == 0)
                    {
                        //Does Ranged Attack
                        this.RangedAttack();
                    }
                    break;
                case 2:
                    if(this.beatCount % this.beatsPerAttack == 0)
                    {
                        //Does Ranged Attack
                        this.RangedAttack();
                    }
                    break;
                case 3:
                    if(this.enterPhase3)
                    {
                        this.KnockDownSetup();
                    }
                    else
                    {
                        if(this.downedTime > 10)
                        {
                            AudioSystem.SetParameter("MusicState", this.curPhase);
                            this.downedTime = 0;
                        }
                        this.downedTime += Time.deltaTime;
                    }
                    break;
                case 4:
                    if(this.enterPhase1)
                    {
                        this.Phase1Setup();
                    }
                    else if(this.beatCount % this.beatsPerAttack == 0)
                    {
                        //Does Ranged Attack
                        this.RangedAttack();
                    }
                    break;
                case 5:
                    if(this.beatCount % this.beatsPerAttack == 0)
                    {
                        //Does Ranged Attack
                        this.RangedAttack();
                    }
                    break;
                case 6:
                    if(this.enterPhase3)
                    {
                        this.KnockDownSetup();
                    }
                    //Wait for player to kill enemy
                    //Will balance it so that players should be able to kill the enemy in this amount of phases
                    break;
            }
        }

        protected override void RangedAttack()
        {
            GameObject projectileInstance = Instantiate(this.projectile, this.gameObject.transform.position, Quaternion.identity);
            projectileInstance.GetComponent<DanceShooterProjectile>().SetP0(this.transform.position);
            int numPlayers = 0;
            foreach(var player in PlayerSystem.Players)
            {
                numPlayers++;
            }
            int playerId = UnityEngine.Random.Range(1, numPlayers + 1);
            Player p = PlayerSystem.Players[playerId - 1];
            projectileInstance.GetComponent<DanceShooterProjectile>().SetPlayer(p);
            this.anim.SetTrigger("LightAttack");
        }

        void Phase1Setup()
        {
            if(this.lerpTime <= 4.0f)
            {
                this.transform.position = new Vector3(this.transform.position.x, Mathf.Lerp(this.transform.position.y, 7, this.lerpTime/4),
                                            this.transform.position.z);
                this.lerpTime += Time.deltaTime;
            }
            else
            {
                this.enterPhase1 = false;
            }
        }

        void KnockDownSetup()
        {
            if(this.downedTime <= 1.0f)
            {
                this.transform.position = new Vector3(this.transform.position.x, Mathf.Lerp(this.transform.position.y, 0, this.downedTime),
                                                    this.transform.position.z);
                this.downedTime += Time.deltaTime;
            }
            else
            {
                this.enterPhase3 = false;
            }
        }
    }
}