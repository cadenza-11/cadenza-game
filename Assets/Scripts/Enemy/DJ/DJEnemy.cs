using DG.Tweening;
using UnityEngine;

namespace Cadenza
{   
    // Thanks Royce
    public class DJEnemy : Enemy
    {
        [SerializeField] private DJCore[] cores;
        [SerializeField] private DroppingVinyl[] droppingVinyls;
        [SerializeField] private SweepingLaser[] sweepingLasers;
        [SerializeField] private ElectrifiedQuadrants electrifiedQuadrants;
        [SerializeField] private int coreMaxHealth;
        [SerializeField] private GameObject forcefield;
        [SerializeField] private Light spotlight;
        [SerializeField] private GameObject electricity;
        [SerializeField] private GameObject healthbarHolder;

        private Phase currentPhase;
        private bool isDown;
        private Material healthMaterial;

        void Start()
        {
            EnemyManager.AddEnemy(this);
            GameManager.PhaseEntered += this.OnPhaseEntered;
            this.healthMaterial = this.healthbarHolder.GetComponent<Renderer>().materials[1];
        }
        
        void OnDestroy()
        {
            GameManager.PhaseEntered -= this.OnPhaseEntered;
        }

        override public void Initialize()
        {
            this.runHealth = (int)(0.2 * this.maxHealth);
            this.hasRun = false;
            this.speed = 0f;
            this.isAttacking = false;
            this.isActionable = true;
        }

        override protected void FixedUpdate(){}

        public override void TakeDamage(int damage)
        {
            if (this.currentPhase.Index > 3) // Final stage
            {
                Debug.Log("Final stage hit!");
                this.currentHealth -= damage;
                this.healthMaterial.SetFloat("_HealthPercent", (float)this.currentHealth / (float)this.maxHealth);
                AudioSystem.PlayOneShotWithParameter(AudioSystem.PlayerOneShotsEvent, "ID", 3, immediate: true);
                if (this.currentHealth > 0)
                    this.anim.SetTrigger("IsHit");
                else
                {
                    foreach (var vinyl in this.droppingVinyls)
                        vinyl.ShutDown();
                    foreach (var laser in this.sweepingLasers)
                        laser.ShutDown();
                    this.anim.SetTrigger("Die");
                    this.electricity.SetActive(true);
                    this.spotlight.DOIntensity(0, 10f).OnComplete(() => EnemyManager.RemoveEnemy(this));
                }
            }
            else if (!this.isDown) return; // Can't damage DJ until he's down
            else if (((float)this.currentHealth / (float)this.maxHealth) > .33f) // Early stages, more than 33% health
            {
                this.currentHealth -= damage;
                this.healthMaterial.SetFloat("_HealthPercent", (float)this.currentHealth / (float)this.maxHealth);
                this.anim.SetTrigger("IsHit");
                AudioSystem.PlayOneShotWithParameter(AudioSystem.PlayerOneShotsEvent, "ID", 3, immediate: true);
            }
            else // Reboot
            {
                // GameManager.RequestNextPhase();
                AudioSystem.SetParameter("MusicState", this.currentPhase.Index + 1);
            }
        }

        void Update()
        {
            if (!this.isDown && this.currentPhase.Index < 4)
            {
                int destroyedCores = 0;
                foreach (DJCore core in this.cores)
                    if (core.IsDestroyed) destroyedCores++;
                if (destroyedCores == 4)
                {
                    // GameManager.RequestNextPhase();
                    AudioSystem.SetParameter("MusicState", this.currentPhase.Index + 1);
                    this.isDown = true;
                }
            }
        }

        private void OnPhaseEntered(Phase phase)
        {
            this.currentPhase = phase;
            switch (phase.Index)
            {
                case 0:
                    this.electricity.SetActive(false);
                    Debug.Log("// DJ PHASE : ONSLAUGHT 1 //");
                    this.OnEarlyOnslaughtStart(this.coreMaxHealth, 15, 2, Color.softBlue);
                    break;
                case 1:
                    Debug.Log("// DJ PHASE : DOWNED 1 //");
                    this.OnDownedStart();
                    break;
                case 2:
                    Debug.Log("// DJ PHASE : ONSLAUGHT 2 //");
                    this.OnEarlyOnslaughtStart((int)((float)this.coreMaxHealth*1.5f), 10, 4, Color.softRed);
                    break;
                case 3:
                    Debug.Log("// DJ PHASE : DOWNED 2 //");
                    this.anim.SetBool("PhaseTwoComplete", true);
                    this.OnDownedStart();
                    break;
                default:
                    Debug.Log("// DJ PHASE : ONSLAUGHT 3 //");
                    Debug.Log($"Phase {this.currentPhase.Index} WIP");
                    this.OnFinalOnslaughtStart();
                    break;
            }
            this.healthMaterial.SetFloat("_HealthPercent", (float)this.currentHealth / (float)this.maxHealth);
        }

        private void OnEarlyOnslaughtStart(int coreHealth, int homingBeats, int shrapnelAmount, Color spotlightColor)
        {
            Debug.Log($"Starting onslaught with core health: {coreHealth} and spotlight color: {spotlightColor}");
            this.forcefield.SetActive(true);
            this.currentHealth = this.maxHealth;
            this.anim.SetBool("IsDowned", false);
            this.anim.SetBool("PhaseTwoComplete", false);
            foreach (var core in this.cores)
                core.Initialize(coreHealth);
            foreach (var vinyl in this.droppingVinyls)
                vinyl.Initialize(homingBeats, shrapnelAmount);
            int laserOffset = 0;
            foreach (var laser in this.sweepingLasers)
            {
                laser.Initialize(homingBeats, laserOffset);
                laserOffset += 5;
            }
            this.electrifiedQuadrants.Initialize(homingBeats, isPhaseThree: false);
            this.isDown = false;
            this.spotlight.intensity = 200;
            this.spotlight.color = spotlightColor;
        }

        private void OnDownedStart()
        {
            Debug.Log("Starting downed phase");
            foreach (var vinyl in this.droppingVinyls)
                vinyl.ShutDown();
            foreach (var laser in this.sweepingLasers)
                laser.ShutDown();
            this.electrifiedQuadrants.ShutDown();
            this.forcefield.SetActive(false);
            this.anim.SetBool("IsDowned", true);
            this.spotlight.intensity = 1000;
            this.spotlight.color = Color.white;
        }

        private void OnFinalOnslaughtStart()
        {
            this.anim.SetBool("IsDowned", false);
            Debug.Log("Starting final onslaught");
            foreach (var vinyl in this.droppingVinyls)
                vinyl.Initialize(5, 8);
            int laserOffset = 0;
            foreach (var laser in this.sweepingLasers)
            {
                laser.Initialize(5, laserOffset);
                laserOffset += 2;
            }
            this.electrifiedQuadrants.Initialize(10, isPhaseThree: true);
            this.currentHealth = this.maxHealth;
            this.spotlight.intensity = 200;
            this.spotlight.color = Color.lightGoldenRodYellow;
        }
    }
}