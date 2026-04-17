using UnityEngine;

namespace Cadenza
{   
    // Thanks Royce
    public class DJEnemy : Enemy
    {
        void Start()
        {
            EnemyManager.AddEnemy(this);
        }

        override public void Initialize()
        {
            this.runHealth = (int)(0.2 * this.maxHealth);
            this.hasRun = false;
            this.speed = 0f;
            this.isAttacking = false;
            this.isActionable = true;
        }
    }
}