using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Cadenza.Combo;

namespace Cadenza
{
    public class Character : MonoBehaviour
    {
        #region Variables
        [Header("Player Values")]
        [SerializeField] public float speed;
        [SerializeField] public float attackDuration;
        [SerializeField] public float maxHealth;

        [Header("Assign in Inspector")]
        [SerializeField] public AttackArea AttackArea;
        [SerializeField] public Rigidbody Rigidbody;
        [SerializeField] public SpriteRenderer Sprite;
        [SerializeField] public Animator Animator;
        [SerializeField] private AccuracyBar accuracyBar;
        [SerializeField] private InteractionIndicator interactionIndicator;
        [SerializeField] public ComboManager comboM;
        [SerializeField] private CharacterClass cClass;
        [SerializeField] public int baseLightDamage;
        [SerializeField] public int baseHeavyDamage;

        [NonSerialized] public float currentHealth;
        public float MaxHealth => this.maxHealth;
        public float FlowThreshold => this.flowThreshold;
        public bool IsFainted => this.isFainted;

        public Player Player { get; private set; }
        public static event Action TeamAttackInitiated;
        public event Action<float> HealthChanged;
        public event Action<float> FlowChanged;

        private float flow;
        private float flowThreshold = 5f;

        public class Input
        {
            public Vector2 move;
            public bool wantLight;
            public bool wantHeavy;
            public bool wantTeam;

            public void Consume()
            {
                this.wantHeavy = false;
                this.wantLight = false;
                this.wantTeam = false;
            }
        }

        public Input input;
        private bool facingRight = true;

        private IState state;
        public readonly WalkingState walking = new();
        public readonly LightAttackState lightAttack = new();
        public readonly HeavyAttackState heavyAttack = new();
        public readonly HitStunState hitStun = new();
        public readonly FaintedState fainted = new();

        private bool isFlowing = false;
        [NonSerialized] public bool isFainted = false;
        #endregion

        internal void Initialize(Player player)
        {
            // Set player.
            this.Player = player;
            player.PlayerHit += this.OnPlayerHit;
            BeatSystem.BeatPlayed += this.UpdateFlowBuffs;
            player.InteractChanged += this.interactionIndicator.OnPlayerInteractChanged;

            this.input = new();
            this.SetHealth(this.maxHealth);
            this.SetFlow(0);

            // Defualt state.
            this.ChangeState(this.walking);
        }

        void Update()
        {
            this.state?.Update(this);
        }

        void FixedUpdate()
        {
            if (!this.IsGrounded())
                this.ApplyGravity();

            this.state?.FixedUpdate(this);

            // Update flow.
            this.SetFlow(this.flow - 0.03f);

            if (this.HasFlowBuff(3))
                this.SetHealth(this.currentHealth + 0.01f);
        }

        void LateUpdate()
        {
            this.input.Consume();
        }

        #region States
        public void ChangeState(IState next)
        {
            if (this.state == next)
                return;

            this.state?.Exit(this);
            this.state = next;
            this.state?.Enter(this);
        }

        #endregion

        #region Utility
        private bool IsGrounded()
        {
            return Physics.Raycast(this.transform.position, Vector3.down, 0.5f);
        }

        private void ApplyGravity()
        {
            this.Rigidbody.AddForce(Physics.gravity, ForceMode.Acceleration);
        }

        public void FlipSpriteFromVelocity(Vector3 velocity)
        {
            if (velocity.x < 0f)
            {
                this.Sprite.flipX = true;
                this.facingRight = false;
            }
            else if (velocity.x > 0f)
            {
                this.Sprite.flipX = false;
                this.facingRight = true;
            }
        }

        public void ManageAttackDirection()
        {
            Vector3 localPos = this.AttackArea.transform.localPosition;
            float absLocalX = Mathf.Abs(localPos.x);
            localPos.x = this.facingRight ? absLocalX : -absLocalX;
            this.AttackArea.transform.localPosition = localPos;
        }

        public bool HasFlowBuff(int idx)
        {
            return this.isFlowing && FlowManager.Singleton.playerFlows[idx];
        }

        #endregion

        #region Combat

        public void StartTeamAttack()
        {
            TeamAttackInitiated?.Invoke();
            AudioSystem.PlayOneShotWithParameter(AudioSystem.PlayerOneShotsEvent, "ID", 4, immediate: false);
        }

        public void DoDamage(int damage)
        {
            this.SetHealth(this.currentHealth - damage);
            this.Animator.SetTrigger("IsHit");
        }

        public void SetHealth(float health)
        {
            this.currentHealth = Mathf.Clamp(this.currentHealth + health, 0.0f, this.maxHealth);
            HealthChanged?.Invoke(this.currentHealth);

            if (this.currentHealth <= 0f)
                this.ChangeState(this.fainted);
            else
                this.ChangeState(this.hitStun.WithDuration(this.attackDuration));
        }

        private void OnPlayerHit(ScoreDef def)
        {
            // Update accuracy.
            this.accuracyBar.OnPlayerHit(def);

            // Update flow.
            float value = def.Class switch
            {
                ScoreClass.Perfect => +3.0f,
                ScoreClass.Great => +1.0f,
                ScoreClass.Bad => -1.0f,
                _ => 0.0f
            };
            this.SetFlow(this.flow + value);
        }

        #endregion

        #region Input
        public void OnMove(InputAction.CallbackContext context)
        {
            this.input.move = context.ReadValue<Vector2>();
        }

        public void OnAttackLight(InputAction.CallbackContext context)
        {
            if (context.performed)
                this.input.wantLight = true;
        }

        public void OnAttackHeavy(InputAction.CallbackContext context)
        {
            if (context.performed)
                this.input.wantHeavy = true;
        }

        public void OnAttackTeam(InputAction.CallbackContext context)
        {
            if (context.performed)
                this.input.wantTeam = true;
        }

        #endregion

        #region Flow

        private void SetFlow(float flow)
        {
            this.flow = Mathf.Clamp(flow, 0.0f, 20.0f);
            FlowChanged?.Invoke(this.flow);
        }

        public void UpdateFlowBuffs()
        {
            if (this.flow >= this.flowThreshold)
            {
                FlowManager.Singleton.playerFlows[this.cClass.ID - 1] = true;
                this.isFlowing = true;
            }
            else
            {
                FlowManager.Singleton.playerFlows[this.cClass.ID - 1] = false;
                this.isFlowing = false;
            }
        }
        #endregion
    }
}
