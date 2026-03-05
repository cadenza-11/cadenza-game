using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Cadenza.Combo;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.XInput;

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
        [SerializeField] public GameObject AttackAreaObject;
        [SerializeField] public Rigidbody Rigidbody;
        [SerializeField] public SpriteRenderer Sprite;
        [SerializeField] public Animator Animator;
        [SerializeField] private AccuracyBar accuracyBar;
        [SerializeField] public ReviveMeter RevivalMeter;
        [SerializeField] private InteractionIndicator interactionIndicator;
        [SerializeField] public ComboManager comboM;
        [SerializeField] public int baseLightDamage;
        [SerializeField] public int baseHeavyDamage;
        [SerializeField] private float flowThreshold;
        [SerializeField] private float reviveThreshold;

        [NonSerialized] public float currentHealth;
        public float MaxHealth => this.maxHealth;
        public float FlowThreshold => this.flowThreshold;
        public bool IsFainted => this.isFainted;
        public IAttackArea AttackArea => this.attackArea;

        public Player Player { get; private set; }
        public static event Action TeamAttackInitiated;
        public event Action<float, bool> HealthChanged;
        public event Action<Character> Died;
        public event Action<float> FlowChanged;
        public event Action Revived;

        private float flow;
        private float revive;

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

        private IAttackArea attackArea;

        private CharacterClass cClass;

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
            this.attackArea = this.AttackAreaObject.GetComponent<IAttackArea>();
            this.cClass = player.CharacterClass;

            this.input = new();
            this.SetHealth(this.maxHealth);
            this.SetFlow(0);

            // Set default state.
            this.ChangeState(this.walking);

            // Set input hints.
            var controller = this.Player.Input.devices[0] switch
            {
                Keyboard or Mouse => ControllerType.Keyboard,
                XInputController => ControllerType.Xbox,
                DualShockGamepad => ControllerType.PlayStation,
                _ => ControllerType.All,
            };
            this.RevivalMeter.SetInputHint(controller);
            this.RevivalMeter.SetThreshold(this.reviveThreshold);
            this.RevivalMeter.Hide();
        }

        void OnDestroy()
        {
            // Unsubscribe from events.
            this.Player.PlayerHit -= this.OnPlayerHit;
            BeatSystem.BeatPlayed -= this.UpdateFlowBuffs;
            this.Player.InteractChanged -= this.interactionIndicator.OnPlayerInteractChanged;
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
            this.SetRevive(this.revive - 0.03f);

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
            Vector3 localPos = this.AttackAreaObject.transform.localPosition;
            float absLocalX = Mathf.Abs(localPos.x);
            localPos.x = this.facingRight ? absLocalX : -absLocalX;
            this.AttackAreaObject.transform.localPosition = localPos;
        }

        public bool HasFlowBuff(int idx)
        {
            return this.isFlowing && TeamSystem.IsClassFlowing(idx);
        }

        #endregion

        #region Combat

        public void StartTeamAttack()
        {
            TeamAttackInitiated?.Invoke();
            AudioSystem.PlayOneShotWithParameter(AudioSystem.PlayerOneShotsEvent, "ID", 4, immediate: false);
        }

        public void TakeDamage(int damage)
        {
            this.SetHealth(this.currentHealth - damage);
        }

        public void SetHealth(float health)
        {
            if (health <= 0f && !this.isFainted)
            {
                this.ChangeState(this.fainted);
                this.Died?.Invoke(this);
            }
            else if (health < this.currentHealth && !this.isFainted)
                this.ChangeState(this.hitStun.WithDuration(this.attackDuration));

            this.currentHealth = Mathf.Clamp(health, 0.0f, this.maxHealth);
            this.HealthChanged?.Invoke(this.currentHealth, this.isFainted);
        }

        private void OnPlayerHit(ScoreDef def)
        {
            // Update accuracy.
            this.accuracyBar.OnPlayerHit(def);

            // Update flow or revive.
            float value = def.Class switch
            {
                ScoreClass.Perfect => +3.0f,
                ScoreClass.Great => +1.0f,
                ScoreClass.Bad => -1.0f,
                _ => 0.0f
            };

            if (this.isFainted)
                this.SetRevive(this.revive + value);
            else
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
            if (context.performed && !this.isFainted)
                this.input.wantLight = true;
        }

        public void OnAttackHeavy(InputAction.CallbackContext context)
        {
            if (context.performed && !this.isFainted)
                this.input.wantHeavy = true;
        }

        public void OnAttackTeam(InputAction.CallbackContext context)
        {
            if (context.performed && !this.isFainted)
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
                TeamSystem.SetClassFlowing(this.cClass.ID, true);
                this.isFlowing = true;
            }
            else
            {
                TeamSystem.SetClassFlowing(this.cClass.ID, false);
                this.isFlowing = false;
            }
        }

        #endregion

        #region Revive

        private void SetRevive(float revive)
        {
            this.revive = Mathf.Clamp(revive, 0.0f, this.reviveThreshold);
            this.RevivalMeter.SetRevive(this.revive);

            if (this.revive >= this.reviveThreshold - 1f)
            {
                this.Revived?.Invoke();
                this.ChangeState(this.walking);
            }
        }

        #endregion
    }
}
