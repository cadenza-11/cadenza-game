using UnityEngine;
using UnityEngine.InputSystem;

namespace Cadenza
{
    public enum AttkEffect
    {
        None,
        Light_Knockback
    }

    public enum AttkTypes
    {
        None,
        Light,
        Heavy
    }
    public class Character : MonoBehaviour, CadenzaActions.IPlayerActions
    {
        [Header("Player Values")]
        [SerializeField] private float speed;
        [SerializeField] private float jumpForce;
        [SerializeField] private float chargeForce;

        [SerializeField] private float attackDuration = 0.25f;
        [SerializeField] private float chargeDuration = 0.5f;

        [Header("Assign in Inspector")]
        [SerializeField] private AttackArea attackArea;
        [SerializeField] private AttackArea chargeArea;
        [SerializeField] private Rigidbody rb;
        [SerializeField] private SpriteRenderer sr;
        [SerializeField] private Animator anim;
        [SerializeField] private AccuracyBar accuracyBar;

        public Player Player { get; private set; }

        private float attackTimer = 0f;
        private float chargeTimer = 0f;
        private int attackMod;

        private Vector2 move;
        private bool isMove, isAttacking, isGrounded, isCharging;
        private bool direction; //true = right, false = left

        private int[] comboArray = new int[2];
        private float comboTimer = 0.0f;
        private bool comboWaiting = false;

        internal void SetPlayer(Player player)
        {
            this.Player = player;
            player.PlayerHit += this.accuracyBar.OnPlayerHit;
        }

        void FixedUpdate()
        {
            //Only adds gravity if not charging
            this.isGrounded = this.CheckIsGrounded();

            if (!this.isCharging)
            {
                this.rb.AddForce(Physics.gravity * 1f, ForceMode.Acceleration);
            }

            //Reads in a Vector2, converts it to a Vector3, and flips sprite based on direction

            if (!this.isCharging)
            {
                Vector3 moveDir = new Vector3(this.move.x * this.speed, this.rb.linearVelocity.y, this.move.y * this.speed);
                this.rb.linearVelocity = moveDir;

                if (moveDir.x != 0 && moveDir.x < 0)
                {
                    this.sr.flipX = true;
                    this.isMove = true;
                    this.direction = false;
                }
                else if (moveDir.x != 0 && moveDir.x > 0)
                {
                    this.sr.flipX = false;
                    this.isMove = true;
                    this.direction = true;
                }
                else if (Mathf.Abs(moveDir.z) > 0)
                {
                    this.isMove = true;
                }
                else if (moveDir.x == 0)
                {
                    this.isMove = false;
                }
            }

            this.anim.SetBool("IsMove", this.isMove);

            //Runs timer so player cant attack more than once (may become an IEnumerator later if more effective)
            if (this.isAttacking && !this.isCharging)
            {
                this.attackTimer += Time.deltaTime;

                if (this.attackTimer >= (this.attackDuration * this.attackMod))
                {
                    this.attackTimer = 0;
                    this.isAttacking = false;
                    this.attackArea.gameObject.SetActive(this.isAttacking);
                }
            }

            if (this.isCharging)
            {
                this.chargeTimer += Time.deltaTime;

                if (this.chargeTimer >= (this.chargeDuration))
                {
                    this.chargeTimer = 0;
                    this.isCharging = false;
                    this.chargeArea.gameObject.SetActive(this.isCharging);
                }
            }

            if (this.comboWaiting)
            {
                this.comboTimer += Time.deltaTime;
            }
        }

        bool CheckIsGrounded()
        {
            //Returns a raycast result to determine if on the ground
            return Physics.Raycast(this.transform.position, -Vector3.up, 0.5f);
        }

        private void JumpCommand()
        {
            //Jump input action command, only jumps if on the ground
            if (this.isGrounded)
            {
                this.rb.AddForce(Vector3.up * this.jumpForce, ForceMode.Impulse);
            }
        }

        /// <summary>
        /// Calculates the absolute x value of the hitbox's vector3 local position, then changes it if the attack is in a different direction
        /// </summary>
        public void ManageAttackDirection()
        {
            Vector3 localPos = this.attackArea.gameObject.transform.localPosition;
            float absLocalX = Mathf.Abs(localPos.x);
            if (this.direction == true)
            {
                localPos.x = absLocalX;
            }
            else if (this.direction == false)
            {
                localPos.x = absLocalX * -1;
            }
            this.attackArea.gameObject.transform.localPosition = localPos;
        }

        #region ICharacter Interface

        private int currentHealth { get; set; }
        private int specialMeter { get; set; }

        private void Move(Vector2 input)
        {
            this.move = input;
        }

        private void WeakAttack(int damage, int comboMove)
        {
            this.ManageAttackDirection();
            //Sets attacking to true and activated the hitbox for the attack
            this.isAttacking = true;
            this.attackMod = 1;
            this.attackArea.damage = damage;
            this.attackArea.comboMove = comboMove;
            this.attackArea.gameObject.SetActive(this.isAttacking);

            // Play animation
            this.anim.SetTrigger("WeakAttack");
        }

        private void SpecialAttack()
        {
            if (this.direction == true)
            {
                this.chargeForce = Mathf.Abs(this.chargeForce);
            }
            else if (this.direction == false)
            {
                this.chargeForce = -Mathf.Abs(this.chargeForce);
            }
            this.isCharging = true;
            this.chargeArea.damage = 10;
            this.chargeArea.SetActive(this.isCharging);
            this.rb.linearVelocity = new Vector3(0.0f, 0.0f, 0.0f);
            this.rb.AddForce(Vector3.right * this.chargeForce, ForceMode.VelocityChange);
        }
        public void StartTeamAttk()
        {

        }
        public void JoinTeamAttk()
        {

        }
        public void DoDamage()
        {

        }

        #endregion
        #region IPlayerActions Interface

        public void OnMove(InputAction.CallbackContext context)
        {
            var input = context.performed ? context.ReadValue<Vector2>() : Vector2.zero;
            this.Move(input);
        }

        public void OnAttackLight(InputAction.CallbackContext context)
        {
            this.ComboManager((int)AttkTypes.Light);
        }

        public void OnAttackSpecial(InputAction.CallbackContext context)
        {
            this.SpecialAttack();
        }

        public void OnAttackTeam(InputAction.CallbackContext context)
        {
            this.StartTeamAttk();
        }

        #endregion

        #region Combo Management
        /// <summary>
        /// All attacks go through the combo manager, which uses a series of switch statememnts to determine what part of the combo the attack falls in.
        /// From there it triggers the attack, with aditional parameters passed for special moves or more damage.
        /// A combo timer set up in update will reset the combo if there is too much time between the last attack and the next.
        /// </summary>
        /// <param name="attType"> The type of attack that is being done (0 for none, 1 for light, 2 for heavy) </param>
        public void ComboManager(int attType)
        {
            for (int i = 0; i < 2; i++)
            {
                if (this.comboArray[i] != 0 && this.comboArray[i] != 1 && this.comboArray[i] != 2)
                {
                    this.comboArray[i] = (int)AttkTypes.None;
                }
            }

            if (this.comboTimer >= 2.0f)
            {
                Debug.Log("Combo Reset");
                this.ResetCombo();
            }
            else
            {
                this.comboTimer = 0.0f;
            }

            switch (attType)
            {
                //No Attack Inputted, reset combo
                case (int)AttkTypes.None:
                    this.ResetCombo();
                    break;

                //Input Light
                case (int)AttkTypes.Light:
                    switch (this.comboArray[0])
                    {
                        //Light -> [None, None, None]
                        case (int)AttkTypes.None:
                            this.comboArray[0] = 1;
                            Debug.Log("[1, 0, 0]");
                            this.comboWaiting = true;
                            this.WeakAttack(3, (int)AttkEffect.None);
                            break;

                        //Light -> [Light, ?, None]
                        case (int)AttkTypes.Light:
                            switch (this.comboArray[1])
                            {
                                //1 -> [Light, None, None]
                                case (int)AttkTypes.None:
                                    this.comboArray[1] = 1;
                                    Debug.Log("[1, 1, 0]");
                                    this.WeakAttack(3, (int)AttkEffect.None);
                                    break;

                                //Light -> [Light, Light, None]
                                case (int)AttkTypes.Light:
                                    Debug.Log("[1, 1, 1]");
                                    this.ResetCombo();
                                    this.WeakAttack(5, (int)AttkEffect.Light_Knockback);
                                    break;

                                //Light -> [Light, Heavy, None]
                                case (int)AttkTypes.Heavy:
                                    this.ResetCombo();
                                    this.WeakAttack(3, (int)AttkEffect.None);
                                    break;
                            }
                            break;

                        case (int)AttkTypes.Heavy:
                            switch (this.comboArray[1])
                            {
                                //Light -> [Heavy, None, None]
                                case (int)AttkTypes.None:
                                    this.comboArray[1] = 1;
                                    this.WeakAttack(3, (int)AttkEffect.None);
                                    break;

                                //Light -> [Heavy, Light, None]
                                case (int)AttkTypes.Light:
                                    this.ResetCombo();
                                    this.WeakAttack(3, (int)AttkEffect.None);
                                    break;

                                //Light -> [Heavy, Heavy, None]
                                case (int)AttkTypes.Heavy:
                                    this.ResetCombo();
                                    this.WeakAttack(3, (int)AttkEffect.None);
                                    break;
                            }
                            break;
                    }
                    break;

                //Input Heavy
                case (int)AttkTypes.Heavy:
                    switch (this.comboArray[0])
                    {
                        //Heavy -> [None, None, None]
                        case (int)AttkTypes.None:
                            this.comboArray[0] = 2;
                            this.comboWaiting = true;
                            //This is where the heavy attack would go
                            break;

                        //Heavy -> [Light, ?, None]
                        case (int)AttkTypes.Light:
                            switch (this.comboArray[1])
                            {
                                //Heavy -> [Light, None, None]
                                case (int)AttkTypes.None:
                                    this.comboArray[1] = 2;
                                    //This is where the heavy attack would go
                                    break;

                                //Heavy -> [Light, Light, None]
                                case (int)AttkTypes.Light:
                                    this.ResetCombo();
                                    //This is where the heavy attack would go
                                    break;

                                //Heavy -> [Light, Heavy, None]
                                case (int)AttkTypes.Heavy:
                                    this.ResetCombo();
                                    //This is where the heavy attack would go
                                    break;
                            }
                            break;

                        case (int)AttkTypes.Heavy:
                            switch (this.comboArray[1])
                            {
                                //Heavy -> [Heavy, None, None]
                                case (int)AttkTypes.None:
                                    this.comboArray[1] = 2;
                                    //This is where the heavy attack would go
                                    break;

                                //Heavy -> [Heavy, Light, None]
                                case (int)AttkTypes.Light:
                                    this.ResetCombo();
                                    //This is where the heavy attack would go
                                    break;

                                //Heavy -> [Heavy, Heavy, None]
                                case (int)AttkTypes.Heavy:
                                    this.ResetCombo();
                                    //This is where the heavy attack would go
                                    break;
                            }
                            break;
                    }
                    break;
            }

        }

        public void ResetCombo()
        {
            this.comboWaiting = false;
            this.comboTimer = 0.0f;
            for (int i = 0; i < 2; i++)
            {
                this.comboArray[i] = (int)AttkTypes.None;
            }
        }

        #endregion
    }
}
