using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Cadenza.Combo;

namespace Cadenza
{
    public class Character : MonoBehaviour, CadenzaActions.IPlayerActions
    {
        #region Variables
        [Header("Player Values")]
        [SerializeField] private float speed;
        [SerializeField] private float jumpForce;
        [SerializeField] private float chargeForce;

        [SerializeField] private float attackDuration = 0.25f;
        [SerializeField] private float chargeDuration = 0.5f;
        [SerializeField] private int currentHealth = 20;
        [SerializeField] private int maxHealth = 20;

        [Header("Assign in Inspector")]
        [SerializeField] private AttackArea attackArea;
        [SerializeField] private AttackArea chargeArea;
        [SerializeField] private AttackArea slamArea;
        [SerializeField] private Rigidbody rb;
        [SerializeField] private SpriteRenderer sr;
        [SerializeField] private Animator anim;
        [SerializeField] private AccuracyBar accuracyBar;
        [SerializeField] private InteractionIndicator interactionIndicator;
        [SerializeField] private GameObject projectile;
        [SerializeField] private ComboManager ComboM;

        public Player Player { get; private set; }
        public static event Action TeamAttackInitiated;
        public event Action<int> HealthChanged;

        private float attackTimer = 0.0f;
        private float chargeTimer = 0.0f;
        private int attackMod;

        private Vector2 move;
        private bool isMove, isAttacking, isGrounded, isCharging;
        private bool direction; //true = right, false = left

        private int[] comboArray = new int[2];
        private float comboTimer = 0.0f;
        private bool comboWaiting = false;

        private int baseLight = 3;
        private int baseHeavy = 6;
        #endregion

        internal void SetPlayer(Player player)
        {
            this.Player = player;
            player.PlayerHit += this.accuracyBar.OnPlayerHit;
            player.InteractChanged += this.interactionIndicator.OnPlayerInteractChanged;
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
                    this.attackTimer = 0.0f;
                    this.isAttacking = false;
                    this.attackArea.gameObject.SetActive(this.isAttacking);
                    this.slamArea.gameObject.SetActive(this.isAttacking);
                }
            }

            if (this.isCharging)
            {
                this.chargeTimer += Time.deltaTime;

                if (this.chargeTimer >= (this.chargeDuration))
                {
                    this.chargeTimer = 0.0f;
                    this.isCharging = false;
                    this.chargeArea.gameObject.SetActive(this.isCharging);
                }
            }

            if (this.comboWaiting)
            {
                this.comboTimer += Time.deltaTime;
            }
        }

        public Vector2 GetLocation()
        {
            Vector3 pos = this.GetComponent<Transform>().position;
            return new Vector2(pos.x, pos.z);
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

        public int GetCurHealth()
        {
            return this.currentHealth;
        }

        public int GetMaxHealth()
        {
            return this.maxHealth;
        }

        #region ICharacter Interface
        private int specialMeter { get; set; }

        private void Move(Vector2 input)
        {
            this.move = input;
        }

        private void LightAttack(int damage, AttkEffect comboMove)
        {
            this.ManageAttackDirection();
            //Sets attacking to true and activated the hitbox for the attack
            this.isAttacking = true;
            this.attackMod = 1;
            this.attackArea.damage = damage;
            this.attackArea.comboMove = comboMove;
            this.attackArea.gameObject.SetActive(this.isAttacking);

            if (comboMove == AttkEffect.AbilityOne)
            {
                this.AbilityOne();
            }
            else if (comboMove == AttkEffect.AbilityTwo)
            {
                this.AbilityTwo();
            }

            // Play animation
            this.anim.SetTrigger("LightAttack");
        }
        private void HeavyAttack(int damage, AttkEffect comboMove)
        {
            this.ManageAttackDirection();
            //Sets attacking to true and activated the hitbox for the attack
            this.isAttacking = true;
            this.attackMod = 2;
            /*
            if (comboMove == AttkEffect.Base_Smash)
            {
                this.slamArea.damage = damage;
                this.slamArea.comboMove = comboMove;
                this.slamArea.gameObject.GetComponent<SphereCollider>().radius = 1;
                this.slamArea.gameObject.SetActive(this.isAttacking);
            }
            else if (comboMove == AttkEffect.Area_Smash)
            {
                this.slamArea.damage = damage;
                this.slamArea.comboMove = comboMove;
                this.slamArea.gameObject.GetComponent<SphereCollider>().radius = 1.5f;
                this.slamArea.gameObject.SetActive(this.isAttacking);
            }
            */
            this.attackArea.damage = damage;
            this.attackArea.comboMove = comboMove;
            this.attackArea.gameObject.SetActive(this.isAttacking);

            if (comboMove == AttkEffect.AbilityOne)
            {
                this.AbilityOne();
            }
            else if (comboMove == AttkEffect.AbilityTwo)
            {
                this.AbilityTwo();
            }

            // Play animation
            this.anim.SetTrigger("HeavyAttack");
        }

        private void SpecialAttack()
        {
            // Code kept just in case, but will be removing special attack at some point
            /*
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
            */
        }
        public void StartTeamAttk()
        {
            TeamAttackInitiated?.Invoke();
            AudioSystem.PlayOneShotWithParameter(AudioSystem.PlayerOneShotsEvent, "ID", 4, immediate: false);
        }
        public void JoinTeamAttk()
        {

        }
        public void DoDamage(int damage)
        {
            this.currentHealth -= damage;
            HealthChanged?.Invoke(this.currentHealth);
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
            ComboM.ProcessCombo(AttkTypes.Light, out var reward);
            this.LightAttack(this.baseLight * reward.Multiplier, reward.AttackEffect);
            //put enums in attack area with namespace
        }

        public void OnAttackHeavy(InputAction.CallbackContext context)
        {
            ComboM.ProcessCombo(AttkTypes.Heavy, out var reward);
            this.HeavyAttack(this.baseHeavy * reward.Multiplier, reward.AttackEffect);
        }

        public void OnAttackSpecial(InputAction.CallbackContext context)
        {
            this.SpecialAttack();
        }

        public void OnAttackTeam(InputAction.CallbackContext context)
        {
            this.StartTeamAttk();
        }

        public void OnPause(InputAction.CallbackContext context)
        {

        }

        #endregion

        public void AbilityOne()
        {
            Debug.Log("Ability 1");
        }

        public void AbilityTwo()
        {
            Debug.Log("Ability 2");
        }
    }
}
