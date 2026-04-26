using UnityEngine;
using System;

//Royce Ortega
namespace Cadenza
{

    public class MainFunkEnemy : Enemy
    {
        #region Variables

        #endregion

        // Do this in Start so that EnemyManager is initialized.
        void Start()
        {
            EnemyManager.AddEnemy(this);
        }


        protected override void FixedUpdate()
        {
            /*
            if (!this.IsGrounded())
            {
                this.rb.AddForce(Physics.gravity * 1f, ForceMode.Acceleration);
            }
            if (this.curAngle * (180 / Math.PI) > -90 && this.curAngle * (180 / Math.PI) < 90)
            {
                // No rotation needed.
                this.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            else
            {
                // Turn character to the left.
                this.transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            */

            if (this.currentHealth <= 0)
            {
                this.DeadState();
            }
            

            //Checks if the Enemy's state needs to change
            //this.CheckState();
        }
    }
}
