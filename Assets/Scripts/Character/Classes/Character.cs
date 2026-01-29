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

        [SerializeField] private float attackDuration;
        [SerializeField] private float chargeDuration;
        [SerializeField] private int currentHealth;
        [SerializeField] private int maxHealth;

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
        [SerializeField] private int baseLightDamage;
        [SerializeField] private int baseHeavyDamage;

        public float MaxHealth => this.maxHealth;
        public bool IsFainted => this.isFainted;

        public Player Player { get; private set; }
        public static event Action TeamAttackInitiated;
        public event Action<int> HealthChanged;

        private float chargeTimer = 0.0f;
        private Coroutine actionableRoutine;
        private int attackMod;
        private float flow = 0;

        private Vector2 move;
        private bool isMove, isAttacking, isCharging;
        private bool direction; //true = right, false = left
        private bool isActionable = true;
        private bool isFainted = false;

        #endregion

        internal void SetPlayer(Player player)
        {
            this.Player = player;
            player.PlayerHit += this.accuracyBar.OnPlayerHit;
            player.PlayerHit += this.UpdateFlow;
            player.InteractChanged += this.interactionIndicator.OnPlayerInteractChanged;
            this.isActionable = true;
        }

        void FixedUpdate()
        {
            // Apply gravity.
            if (!this.IsGrounded() && !this.isCharging)
            {
                this.rb.AddForce(Physics.gravity * 1f, ForceMode.Acceleration);
            }

            //Reads in a Vector2, converts it to a Vector3, and flips sprite based on direction

            if (this.isActionable && !this.isCharging)
            {
                Vector3 moveDir = new(
                    this.move.x * this.speed,
                    this.rb.linearVelocity.y,
                    this.move.y * this.speed);
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
                this.anim.SetBool("IsMove", this.isMove);
            }

            if (this.isCharging)
            {
                this.chargeTimer += Time.deltaTime;

                if (this.chargeTimer >= this.chargeDuration)
                {
                    this.chargeTimer = 0.0f;
                    this.isCharging = false;
                    this.chargeArea.gameObject.SetActive(this.isCharging);
                }
            }

            if (this.flow > 0.0f && this.flow <= 20.0f)
            {
                this.flow -= 0.03f;
            }
            else if (this.flow > 20.0f)
            {
                this.flow = 20.0f;
            }
            else
            {
                this.flow = 0.0f;
            }
        }

        private bool IsGrounded()
        {
            return Physics.Raycast(this.transform.position, Vector3.down, 0.5f);
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

        private bool TryPerformAction(float duration)
        {
            if (!this.isActionable)
                return false;

            // Perform action.
            this.isActionable = false;
            this.rb.linearVelocity = new Vector3(0, this.rb.linearVelocity.y, 0);
            {
                if (this.actionableRoutine != null)
                    this.StopCoroutine(this.actionableRoutine);
                this.actionableRoutine = this.Schedule(duration, () => this.isActionable = true);
            }
            return true;
        }

        #region ICharacter Interface

        private void LightAttack(int damage, AttkEffect comboMove)
        {
            if (this.isAttacking || !this.TryPerformAction(this.attackDuration * this.attackMod))
                return;

            this.ManageAttackDirection();
            //Sets attacking to true and activated the hitbox for the attack
            this.isAttacking = true;
            this.attackMod = 1;
            this.attackArea.damage = damage;
            this.attackArea.comboMove = comboMove;
            this.attackArea.gameObject.SetActive(this.isAttacking);

            this.Schedule(this.attackDuration * this.attackMod, () =>
            {
                this.isAttacking = false;
                this.attackArea.gameObject.SetActive(this.isAttacking);
                this.slamArea.gameObject.SetActive(this.isAttacking);
            });

            // Play animation
            this.anim.SetTrigger("LightAttack");
        }
        private void HeavyAttack(int damage, AttkEffect comboMove)
        {
            if (this.isAttacking || !this.TryPerformAction(this.attackDuration))
                return;

            this.ManageAttackDirection();
            //Sets attacking to true and activated the hitbox for the attack
            this.isAttacking = true;
            this.attackMod = 2;

            this.attackArea.damage = damage;
            this.attackArea.comboMove = comboMove;
            this.attackArea.gameObject.SetActive(this.isAttacking);

            this.Schedule(this.attackDuration * this.attackMod, () =>
            {
                this.isAttacking = false;
                this.attackArea.gameObject.SetActive(this.isAttacking);
                this.slamArea.gameObject.SetActive(this.isAttacking);
            });

            // Play animation
            this.anim.SetTrigger("HeavyAttack");
        }

        private void SpecialAttack()
        {

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
            this.anim.SetTrigger("IsHit");

            // Hit stun.
            this.isActionable = false;
            this.rb.linearVelocity = new Vector3(0, this.rb.linearVelocity.y, 0);
            {
                if (this.actionableRoutine != null)
                    this.StopCoroutine(this.actionableRoutine);
                this.actionableRoutine = this.Schedule(this.attackDuration, () => this.isActionable = true);
            }

            if (this.currentHealth <= 0)
            {
                // Faint.
                this.isActionable = false;
                this.rb.linearVelocity = new Vector3(0, this.rb.linearVelocity.y, 0);
                this.isFainted = true;
                this.anim.SetBool("IsFainted", true);

                // TEMP: Respawn with full health.
                if (this.actionableRoutine != null)
                    this.StopCoroutine(this.actionableRoutine);
                this.actionableRoutine = this.Schedule(2.0f, () =>
                {
                    this.isActionable = true;
                    this.isFainted = false;
                    this.anim.SetBool("IsFainted", false);
                    this.currentHealth = this.maxHealth;
                    HealthChanged?.Invoke(this.currentHealth);
                });
            }

            HealthChanged?.Invoke(this.currentHealth);
        }

        #endregion
        #region IPlayerActions Interface

        public void OnMove(InputAction.CallbackContext context)
        {
            this.move = context.ReadValue<Vector2>();
        }

        public void OnAttackLight(InputAction.CallbackContext context)
        {
            this.ComboM.ProcessCombo(AttkTypes.Light, out var reward);
            this.LightAttack(this.baseLightDamage * reward.Multiplier, reward.AttackEffect);
            //put enums in attack area with namespace
        }

        public void OnAttackHeavy(InputAction.CallbackContext context)
        {
            this.ComboM.ProcessCombo(AttkTypes.Heavy, out var reward);
            this.HeavyAttack(this.baseHeavyDamage * reward.Multiplier, reward.AttackEffect);
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

        public void UpdateFlow(ScoreDef def)
        {
            switch (def.Class)
            {
                case (ScoreClass.Perfect):
                    this.flow += 3.0f;
                    break;
                case (ScoreClass.Great):
                    this.flow++;
                    break;
                case (ScoreClass.Bad):
                    this.flow--;
                    break;
            }
        }
    }
}
