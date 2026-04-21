using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Cadenza.Combo;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.XInput;
using UnityEngine.VFX;
using Cadenza.Utils;

namespace Cadenza
{
    public class Character : MonoBehaviour
    {
        #region Variables
        [Header("Player Values")]
        [SerializeField] public float speed;
        [SerializeField] public float attackDuration;
        [SerializeField] public float baseHealth;

        [Header("Assign in Inspector")]
        [SerializeField] public AttackArea AttackArea;
        [SerializeField] public Rigidbody Rigidbody;
        [SerializeField] public SpriteRenderer Sprite;
        [SerializeField] public Transform SpriteTransform;
        [SerializeField] public VisualEffect ChargeEffect;
        [SerializeField] public Animator Animator;
        [SerializeField] private AccuracyBar accuracyBar;
        [SerializeField] public ReviveMeter RevivalMeter;
        [SerializeField] private InteractionIndicator interactionIndicator;
        [SerializeField] private SelectionCircle selectionCircle;
        [SerializeField] private PlayerNameIndicator playerNameIndicator;
        [SerializeField] public ComboManager comboM;
        [SerializeField] public int baseLightDamage;
        [SerializeField] public int baseHeavyDamage;
        [SerializeField] private float flowThreshold;
        [SerializeField] private float reviveThreshold;
        private float maxHealth;

        [NonSerialized] public float currentHealth;
        public float MaxHealth => this.maxHealth;
        public float FlowThreshold => this.flowThreshold;
        public bool IsFainted => this.isFainted;
        public bool FacingRight => this.facingRight;
        public int ChargeBeatsPassed { get; internal set; }

        public Player Player { get; private set; }
        public static event Action TeamAttackInitiated;
        public event Action<float, bool> HealthChanged;
        public event Action<Character> Died;
        public event Action<float> FlowChanged;
        public event Action Revived;
        public event Action Parried;

        private float flow;
        private float revive;
        private float parryActiveUntil = float.NegativeInfinity;
        private float pcFactor;

        public class Input
        {
            public Vector2 move;
            public ScoreDef? lightAttack;
            public ScoreDef? heavyAttack;
            public ScoreDef? parry;
            public ScoreDef? charge;
            public bool wantTeam;

            public void Consume()
            {
                this.lightAttack = null;
                this.heavyAttack = null;
                this.parry = null;
                this.charge = null;
                this.wantTeam = false;
            }
        }

        public Input input;
        private bool facingRight = true;

        private IState state;
        public IState CurrentState => this.state;
        public readonly WalkingState walking = new();
        public readonly LightAttackState lightAttack = new();
        public readonly ChargingState charging = new();
        public readonly HeavyAttackState heavyAttack = new();
        public readonly ParryState parry = new();
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

            //Leaving this in in case it would be useful later
            //Old attempt at player stat-based balancing, will be moving to enemy stat-based balancing
            this.pcFactor = 1; // 4 / PlayerSystem.PlayerCount;
            this.maxHealth = this.baseHealth * this.pcFactor;

            this.input = new();
            this.SetHealth(this.maxHealth);
            this.SetFlow(0);

            // Set name.
            this.playerNameIndicator.SetName(player.Name);

            // Set sprite colors (shader).
            this.Sprite.material.SetInt("_CharacterColor", 1);
            if (this.Player.Colorway != null)
            {
                this.Sprite.material.SetColor("_PrimaryColor", this.Player.Colorway.PrimaryColor);
                this.Sprite.material.SetColor("_SecondaryColor", this.Player.Colorway.SecondaryColor);
                this.Sprite.material.SetColor("_TertiaryColor", this.Player.Colorway.TertiaryColor);
            }

            // Set selection circle color.
            this.selectionCircle?.ApplyColorway(this.Player.Colorway);
            this.selectionCircle?.Show();

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
            Character.TeamAttackInitiated += this.TeamAttackEffect;
        }

        void OnDestroy()
        {
            // Unsubscribe from events.
            if (this.Player != null)
            {
                this.Player.InteractChanged -= this.interactionIndicator.OnPlayerInteractChanged;
            }
            Character.TeamAttackInitiated -= this.TeamAttackEffect;
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

        public void UpdateGroundMovement()
        {
            int flowSpeed = this.HasFlowBuff(0) ? 1 : 0;
            float speedModifier = this.speed * (1 + (0.25f * flowSpeed));

            Vector3 velocity = new(
                this.input.move.x * speedModifier,
                this.Rigidbody.linearVelocity.y,
                this.input.move.y * speedModifier
            );

            this.Rigidbody.linearVelocity = velocity;

            bool moving = Mathf.Abs(velocity.x) > 0.001f || Mathf.Abs(velocity.z) > 0.001f;
            this.Animator.SetBool("IsMove", moving);

            if (Mathf.Abs(velocity.x) > 0.001f)
                this.FlipSpriteFromVelocity(velocity);
        }

        public void StopGroundMovement()
        {
            this.Rigidbody.linearVelocity = Vector3.zero;
            this.Animator.SetBool("IsMove", false);
        }

        public bool HasFlowBuff(int idx)
        {
            return this.isFlowing && TeamSystem.IsClassFlowing(idx);
        }

        #endregion
        #region Combat

        public void StartTeamAttack()
        {
            if (UISystem.FindPanel<HUD>().TeamMeter.value >= UISystem.FindPanel<HUD>().TeamMeter.highValue)
            {
                TeamAttackInitiated?.Invoke();
                AudioSystem.PlayOneShotWithParameter(AudioSystem.PlayerOneShotsEvent, "ID", 4, immediate: false);
            }
            //put in a negative sound effect if its not full
        }

        public void TeamAttackEffect()
        {
            this.SetFlow(20);
        }

        public bool TakeDamage(int damage)
        {
            // Parry.
            if (this.IsParrying())
            {
                this.Parried?.Invoke();
                this.ClearParryWindow();
                return false;
            }

            // Lessen damage.
            float fDamage = damage;
            if (this.HasFlowBuff(1))
                fDamage *= 0.8f;

            // Take damage.
            this.SetHealth(this.currentHealth - fDamage);
            return true;
        }

        public void SetHealth(float health)
        {
            this.Sprite.material.SetInt("_Debuff", (health / this.maxHealth <= 0.33f) ? 1 : 0);
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

        public void OnCrowdSurf(bool isSurfing)
        {
            this.SpriteTransform.rotation = Quaternion.Euler(0, 0, isSurfing ? 60f : 0f);
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

        public void OnAttackCharge(ScoreDef score)
        {
            this.input.charge = score;
        }

        public void OnAttackHeavy(ScoreDef score)
        {
            this.input.heavyAttack = score;
        }

        public void OnAttackTeam()
        {
            this.input.wantTeam = true;
        }

        public void OnParry(ScoreDef score)
        {
            this.input.parry = score;
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
        #region Parry

        internal void ActivateParryWindow(float seconds)
        {
            this.parryActiveUntil = Time.time + Mathf.Max(0.0f, seconds);
        }

        internal void ClearParryWindow()
        {
            this.parryActiveUntil = float.NegativeInfinity;
        }

        internal bool IsParrying()
        {
            return Time.time <= this.parryActiveUntil;
        }

        #endregion
    }
}
