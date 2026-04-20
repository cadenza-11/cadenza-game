using UnityEngine;

namespace Cadenza
{   
    // Thanks Royce
    public class DJEnemy : Enemy
    {
        [SerializeField] private DJCore[] cores;
        [SerializeField] private int coreMaxHealth;
        [SerializeField] private GameObject forcefield;
        [SerializeField] private Light spotlight;
        [SerializeField] private GameObject healthbarHolder;

        private Phase currentPhase;
        private bool isDown;
        private Material healthMaterial;

        void Start()
        {
            EnemyManager.AddEnemy(this);
            GameManager.PhaseEntered += this.OnPhaseEntered;
        }

        override public void Initialize()
        {
            this.healthMaterial = this.healthbarHolder.GetComponent<Renderer>().materials[1];
            this.runHealth = (int)(0.2 * this.maxHealth);
            this.hasRun = false;
            this.speed = 0f;
            this.isAttacking = false;
            this.isActionable = true;
        }

        override protected void FixedUpdate(){}

        public override void TakeDamage(int damage)
        {
            Debug.Log($"DJ taking {damage} damage. isDown: {this.isDown}, currentHealth: {this.currentHealth}, maxHealth: {this.maxHealth}, currentPhase: {this.currentPhase.Index}");
            if (this.currentPhase.Index > 3) // Final stage
            {
                Debug.Log("Final stage hit!");
                this.currentHealth -= damage;
                this.healthMaterial.SetFloat("_HealthPercent", (float)this.currentHealth / (float)this.maxHealth);
                this.anim.SetTrigger("IsHit");
                AudioSystem.PlayOneShotWithParameter(AudioSystem.PlayerOneShotsEvent, "ID", 3, immediate: true);
                if (this.currentHealth <= 0)
                {
                    // Faint anim
                    EnemyManager.RemoveEnemy(this);
                }
            }
            else if (!this.isDown)
            {
                Debug.Log("DJ not downed. Can't take damage.");
                return; // Can't damage DJ until he's down
            }
            else if (((float)this.currentHealth / (float)this.maxHealth) > .33f) // Early stages, more than 33% health
            {
                Debug.Log("DJ hit but not downed. Health above 33%.");
                this.currentHealth -= damage;
                this.healthMaterial.SetFloat("_HealthPercent", (float)this.currentHealth / (float)this.maxHealth);
                this.anim.SetTrigger("IsHit");
                AudioSystem.PlayOneShotWithParameter(AudioSystem.PlayerOneShotsEvent, "ID", 3, immediate: true);
            }
            else // Reboot
            {
                Debug.Log("DJ killed during downed phase. Initiating reboot.");
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
                    Debug.Log("// DJ PHASE : ONSLAUGHT 1 //");
                    this.forcefield.SetActive(true);
                    this.currentHealth = this.maxHealth;
                    this.isDown = false;
                    this.anim.SetBool("IsDowned", false);
                    this.anim.SetBool("PhaseTwoComplete", false);
                    foreach (var core in this.cores)
                        core.Initialize(this.coreMaxHealth);
                    this.spotlight.intensity = 200;
                    this.spotlight.color = Color.softBlue;
                    break;
                case 1:
                    Debug.Log("// DJ PHASE : DOWNED 1 //");
                    this.forcefield.SetActive(false);
                    this.anim.SetBool("IsDowned", true);
                    this.spotlight.intensity = 1000;
                    this.spotlight.color = Color.white;
                    break;
                case 2:
                    Debug.Log("// DJ PHASE : ONSLAUGHT 2 //");
                    this.forcefield.SetActive(true);
                    this.currentHealth = this.maxHealth;
                    this.isDown = false;
                    this.anim.SetBool("IsDowned", false);
                    foreach (var core in this.cores)
                        core.Initialize((int)((float)this.coreMaxHealth*1.5f));
                    this.spotlight.intensity = 200;
                    this.spotlight.color = Color.softRed;
                    break;
                case 3:
                    Debug.Log("// DJ PHASE : DOWNED 2 //");
                    this.forcefield.SetActive(false);
                    this.anim.SetBool("IsDowned", true);
                    this.anim.SetBool("PhaseTwoComplete", true);
                    this.spotlight.intensity = 200;
                    this.spotlight.color = Color.white;
                    break;
                default:
                    Debug.Log("// DJ PHASE : ONSLAUGHT 3 //");
                    this.currentHealth = this.maxHealth;
                    Debug.Log($"Phase {phase.Index} WIP");
                    this.spotlight.intensity = 200;
                    this.spotlight.color = Color.lightGoldenRodYellow;
                    break;
            }
            this.healthMaterial.SetFloat("_HealthPercent", (float)this.currentHealth / (float)this.maxHealth);
        }
    }
}