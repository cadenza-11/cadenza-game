using UnityEngine;
using UnityEngine.InputSystem;

namespace Cadenza
{
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

        internal void SetPlayer(Player player)
        {
            this.Player = player;
            player.PlayerHit += this.accuracyBar.OnPlayerHit;
        }

        void FixedUpdate()
        {
            //Only adds gravity if in air
            this.isGrounded = this.CheckIsGrounded();

            if (!this.isGrounded && !this.isCharging)
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
                    this.attackArea.SetActive(this.isAttacking);
                }
            }

            if (this.isCharging)
            {
                this.chargeTimer += Time.deltaTime;

                if (this.chargeTimer >= (this.chargeDuration))
                {
                    this.chargeTimer = 0;
                    this.isCharging = false;
                    this.chargeArea.SetActive(this.isCharging);
                }
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

        #region ICharacter Interface

        private int currentHealth { get; set; }
        private int specialMeter { get; set; }

        private void Move(Vector2 input)
        {
            this.move = input;
        }

        private void WeakAttack()
        {
            //Sets attacking to true and activated the hitbox for the attack
            this.isAttacking = true;
            this.attackMod = 1;
            this.attackArea.damage = 3;
            this.attackArea.SetActive(this.isAttacking);

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
            this.WeakAttack();
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
    }
}
