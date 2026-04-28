using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Cadenza.Combo;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.XInput;
using UnityEngine.VFX;

namespace Cadenza
{
    public class Character : MonoBehaviour
    {
        #region Variables
        [Header("Player Values")]
        [SerializeField] public float speed;
        [SerializeField] public float attackDuration;
        [SerializeField] public float baseHealth;
        [SerializeField] private float postHitInvulnerabilityBeats;
        [SerializeField] private float minimumInvulnerabilityTime;

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
        [SerializeField] private GameObject teamWave;
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
        private bool isInvulnerable;
        private int lastReservedCombatBeat = int.MinValue;
        private bool hasAcceptedCharge;
        private int acceptedChargeBeat = int.MinValue;

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
            this.lastReservedCombatBeat = int.MinValue;
            this.hasAcceptedCharge = false;
            this.acceptedChargeBeat = int.MinValue;
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
            BeatSystem.BeatPlayed += this.OnBeat;
        }

        void OnDestroy()
        {
            // Unsubscribe from events.
            if (this.Player != null)
            {
                this.Player.InteractChanged -= this.interactionIndicator.OnPlayerInteractChanged;
            }
            Character.TeamAttackInitiated -= this.TeamAttackEffect;
            BeatSystem.BeatPlayed -= this.OnBeat;
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

            if (this.HasFlowBuff(3) && !this.isFainted)
                this.SetHealth(this.currentHealth + 0.01f);
        }

        void LateUpdate()
        {
            this.input.Consume();
        }

        private void OnBeat()
        {
            this.SetFlow(this.flow - 0.75f);
            this.SetRevive(this.revive - 0.75f);
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
            Vector3 newVelocity = this.Rigidbody.linearVelocity;
            newVelocity.x = 0;
            newVelocity.z = 0;
            this.Rigidbody.linearVelocity = newVelocity;
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
            Instantiate(this.teamWave, this.gameObject.transform.position, Quaternion.identity);
        }

        public bool TakeDamage(int damage)
        {
            if (this.isInvulnerable)
                return false;

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
            {
                this.ChangeState(this.hitStun.WithDuration(this.attackDuration));

                // Give i-frames.
                float invulnerabilitySeconds = Mathf.Max(
                    this.minimumInvulnerabilityTime,
                    (float)BeatSystem.SecondsPerBeat * this.postHitInvulnerabilityBeats);
                this.BeginInvulnerability(invulnerabilitySeconds);
            }

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

        public bool OnAttackLight(ScoreDef score)
        {
            if (this.state != this.walking && this.state != this.fainted)
                return false;

            if (!this.TryReserveCombatBeat(score.Beat))
                return false;

            this.input.lightAttack = score;
            return true;
        }

        public bool OnAttackCharge(ScoreDef score)
        {
            if (this.state != this.walking)
                return false;

            if (!this.TryReserveCombatBeat(score.Beat))
                return false;

            this.input.charge = score;
            this.hasAcceptedCharge = true;
            this.acceptedChargeBeat = score.Beat;
            return true;
        }

        public bool OnAttackHeavy(ScoreDef score)
        {
            if (this.state == this.fainted)
            {
                if (!this.TryReserveCombatBeat(score.Beat))
                    return false;

                this.input.heavyAttack = score;
                return true;
            }

            if (!this.hasAcceptedCharge)
                return false;

            if (score.Beat == this.acceptedChargeBeat || score.Beat == this.lastReservedCombatBeat)
            {
                this.CancelCharge();
                return false;
            }

            if (this.state != this.charging && this.state != this.walking)
            {
                this.CancelCharge();
                return false;
            }

            if (!this.TryReserveCombatBeat(score.Beat))
            {
                this.CancelCharge();
                return false;
            }

            this.input.charge = null;
            this.input.heavyAttack = score;
            this.hasAcceptedCharge = false;
            this.acceptedChargeBeat = int.MinValue;
            return true;
        }

        public void OnAttackTeam()
        {
            this.input.wantTeam = true;
        }

        public bool OnParry(ScoreDef score)
        {
            if (this.state != this.walking)
                return false;

            if (!this.TryReserveCombatBeat(score.Beat))
                return false;

            this.input.parry = score;
            return true;
        }

        internal void ProcessHeldChargeBeat(int beat)
        {
            if (this.state != this.charging || !this.hasAcceptedCharge)
                return;

            if (!this.TryReserveCombatBeat(beat))
                return;

            this.comboM.ProcessHeldCharge(beat);
            this.ChargeBeatsPassed++;
        }

        internal void ClearChargeTracking()
        {
            this.hasAcceptedCharge = false;
            this.acceptedChargeBeat = int.MinValue;
        }

        private bool TryReserveCombatBeat(int beat)
        {
            if (this.lastReservedCombatBeat == beat)
                return false;

            this.lastReservedCombatBeat = beat;
            return true;
        }

        private void CancelCharge()
        {
            this.input.charge = null;
            this.input.heavyAttack = null;
            this.ClearChargeTracking();

            if (this.state == this.charging)
                this.ChangeState(this.walking);
        }

        #endregion
        #region Flow

        private void SetFlow(float flow)
        {
            this.flow = Mathf.Clamp(flow, 0.0f, 20.0f);

            // Set flow buffs.
            bool isFlowing = this.flow >= this.flowThreshold;
            TeamSystem.SetClassFlowing(this.cClass.ID, isFlowing);
            this.isFlowing = isFlowing;

            // Set shader.
            this.Sprite.material.SetInt("_Flowstate", this.isFlowing ? 1 : 0);
            if (this.isFlowing)
                this.Sprite.material.SetFloat("_LineThickness", (this.flow - this.flowThreshold) / 1000);

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
        #region Invulnerability

        private void BeginInvulnerability(float seconds)
        {
            this.isInvulnerable = true;
            this.Sprite.material.SetInt("_Damage", 1);
            this.Schedule(seconds, this.ClearInvulnerability);
        }

        private void ClearInvulnerability()
        {
            this.isInvulnerable = false;
            this.Sprite.material.SetInt("_Damage", 0);
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
