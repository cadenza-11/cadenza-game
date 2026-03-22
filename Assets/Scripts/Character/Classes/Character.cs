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
        [SerializeField] public AttackArea AttackArea;
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
            public ScoreDef? lightAttack;
            public ScoreDef? heavyAttack;
            public bool wantTeam;

            public void Consume()
            {
                this.lightAttack = null;
                this.heavyAttack = null;
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

        private CharacterClass cClass;

        private bool isFlowing = false;
        [NonSerialized] public bool isFainted = false;
        #endregion

        internal void Initialize(Player player)
        {
            // Set player.
            this.Player = player;
            player.InteractChanged += this.interactionIndicator.OnPlayerInteractChanged;
            this.cClass = player.CharacterClass;

            this.input = new();
            this.SetHealth(this.maxHealth);
            this.SetFlow(0);

            // Set sprite colors (shader).
            if (this.cClass.Name == "Guitar") // TEMP
                this.Sprite.material.SetInt("_CharacterColor", 1);
            if (this.Player.Colorway != null)
            {
                this.Sprite.material.SetColor("_PrimaryColor", this.Player.Colorway.PrimaryColor);
                this.Sprite.material.SetColor("_SecondaryColor", this.Player.Colorway.SecondaryColor);
                this.Sprite.material.SetColor("_TertiaryColor", this.Player.Colorway.TertiaryColor);
            }

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
            this.interactionIndicator.SetInputHint(controller);
            this.RevivalMeter.SetInputHint(controller);
            this.RevivalMeter.SetThreshold(this.reviveThreshold);
            this.RevivalMeter.Hide();
        }

        void OnDestroy()
        {
            // Unsubscribe from events.
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
            Vector3 localPos = this.AttackArea.transform.localPosition;
            float absLocalX = Mathf.Abs(localPos.x);
            localPos.x = this.facingRight ? absLocalX : -absLocalX;
            this.AttackArea.transform.localPosition = localPos;
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
            float fDamage = damage;
            if (this.HasFlowBuff(1))
            {
                fDamage *= 0.8f;
            }
            this.SetHealth(this.currentHealth - fDamage);
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

        public void UpdateAccuracy(ScoreDef def)
        {
            // Update accuracy.
            this.accuracyBar.OnPlayerHit(def);
        }

        public void UpdateFlow(ScoreDef def, float multiplier = 1f)
        {
            // Update flow.
            float value = multiplier * def.Class switch
            {
                ScoreClass.Perfect => +3.0f,
                ScoreClass.Great => +1.0f,
                ScoreClass.Bad => -1.0f,
                _ => 0.0f
            };
            this.SetFlow(this.flow + value);
        }

        public void UpdateRevive(ScoreDef def, float multiplier = 1f)
        {
            if (!this.isFainted)
                return;

            // Update revive.
            float value = multiplier * def.Class switch
            {
                ScoreClass.Perfect => +3.0f,
                ScoreClass.Great => +1.0f,
                ScoreClass.Bad => -1.0f,
                _ => 0.0f
            };
            this.SetRevive(this.revive + value);
        }

        public void OnAllyHit(ScoreDef def)
        {
            // Use ally hit to revive this character.
            // Ally revives are worth more than self revives.
            if (this.isFainted)
                this.UpdateRevive(def, multiplier: 2);
        }

        #endregion

        #region Input
        public void OnMove(Vector2 move)
        {
            this.input.move = move;
        }

        public void OnAttackLight(ScoreDef score)
        {
            this.input.lightAttack = score;
        }

        public void OnAttackHeavy(ScoreDef score)
        {
            this.input.heavyAttack = score;
        }

        public void OnAttackTeam()
        {
            this.input.wantTeam = true;
        }

        #endregion

        #region Flow

        private void SetFlow(float flow)
        {
            this.flow = Mathf.Clamp(flow, 0.0f, 20.0f);

            // Set flow buffs.
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

            // Set shader.
            this.Sprite.material.SetInt("_Flowstate", this.isFlowing ? 1 : 0);
            if (this.isFlowing)
                this.Sprite.material.SetFloat("_LineThickness", (this.flow - this.flowThreshold) / 1000);

            // Set audio.
            AudioSystem.SetParameter(this.cClass.Name, this.flow / this.flowThreshold);

            FlowChanged?.Invoke(this.flow);
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
