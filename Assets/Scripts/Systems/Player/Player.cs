using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cadenza
{
    public class Player : MonoBehaviour
    {
        #region Public Variables

        public int ID { get; private set; }
        public CharacterClass CharacterClass { get; private set; }
        public Character Character { get; private set; }
        public PlayerInput Input { get; private set; }
        public Colorway Colorway { get; private set; }
        public string Name;
        public double Latency => ScoreSystem.GetInputLatencyForPlayer(this);

        public event Action<ScoreDef> PlayerHit;
        public event Action<IInteractable> InteractChanged;

        #endregion

        private InputAction interactAction;
        private Action<InputAction.CallbackContext> interactCallback;
        private IInteractable currentInteractable;

        private void OnDestroy()
        {
            if (this.Character != null)
                this.UnregisterCharacterCallbacks(this.Input.actions);
        }

        #region Public Methods

        internal void Initialize(int id, PlayerInput input)
        {
            this.ID = id;
            this.Input = input;
            this.Name = $"Player {id + 1}";
            this.Colorway = null;
        }

        internal void SetCharacterClass(CharacterClass characterClass)
        {
            this.CharacterClass = characterClass;
        }

        /// <summary>
        /// Tracks this player to an instance of a character body.
        /// </summary>
        /// <param name="character">A spawned instance of the player body.</param>
        internal void SetCharacter(Character character)
        {
            // Remove the character body.
            if (character == null && this.Character != null)
            {
                this.UnregisterCharacterCallbacks(this.Input.actions);
                Destroy(this.Character);
            }

            this.Character = character;

            // Give input to new character body.
            if (this.Character != null)
            {
                this.Character.Initialize(this);
                this.RegisterCharacterCallbacks(this.Input.actions);
            }
        }

        public void RegisterInteract(IInteractable interactable)
        {
            if (interactable == null || interactable == this.currentInteractable)
                return;

            if (this.currentInteractable != null)
                this.UnregisterInteract(this.currentInteractable);

            // Subscribe the interactable to the interact action.
            this.currentInteractable = interactable;
            this.interactCallback = ctx => interactable?.OnInteract(this);
            this.interactAction.performed += this.interactCallback;

            this.InteractChanged?.Invoke(interactable);
        }

        public void UnregisterInteract(IInteractable interactable)
        {
            if (interactable == null || interactable != this.currentInteractable)
                return;

            // Unsubscribe the interactable from the interact action.
            this.currentInteractable = null;
            this.interactAction.performed -= this.interactCallback;
            this.interactCallback = null;

            this.InteractChanged?.Invoke(null);
        }

        public void SetColorway(Colorway colorway)
        {
            this.Colorway = colorway;
        }

        #endregion
        #region Input
        private void RegisterCharacterCallbacks(InputActionAsset actionMaps)
        {
            // Player map.
            var map = actionMaps.FindActionMap("Player", throwIfNotFound: true);

            var moveAction = map.FindAction("Move", throwIfNotFound: true);
            var attackLightAction = map.FindAction("Attack/Light", throwIfNotFound: true);
            var attackHeavyAction = map.FindAction("Attack/Heavy", throwIfNotFound: true);
            var attackTeamAction = map.FindAction("Attack/Team", throwIfNotFound: true);
            var blockAction = map.FindAction("Block", throwIfNotFound: true);
            var pauseAction = map.FindAction("Pause", throwIfNotFound: true);
            this.interactAction = attackLightAction;

            moveAction.performed += this.OnMove;
            moveAction.canceled += this.OnMove;
            attackLightAction.performed += this.OnAttackLight;
            attackHeavyAction.performed += this.OnAttackCharge;
            attackHeavyAction.canceled += this.OnAttackHeavy;
            attackTeamAction.performed += this.OnAttackTeam;
            blockAction.performed += this.OnBlockPressed;
            blockAction.canceled += this.OnBlockReleased;
            pauseAction.performed += this.OnPause;
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            if (this.Character != null)
                this.Character.OnMove(context.ReadValue<Vector2>());
        }

        private void OnAttackLight(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            var score = ScoreSystem.GetScore(BeatSystem.CurrentTrackTime, this);
            if (this.Character != null && this.Character.OnAttackLight(score))
                this.PlayerHit?.Invoke(score);
        }

        private void OnAttackHeavy(InputAction.CallbackContext context)
        {
            if (!context.canceled)
                return;

            var score = ScoreSystem.GetScore(BeatSystem.CurrentTrackTime, this);
            if (this.Character != null && this.Character.OnAttackHeavy(score))
                this.PlayerHit?.Invoke(score);
        }

        private void OnAttackCharge(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            var score = ScoreSystem.GetScore(BeatSystem.CurrentTrackTime, this);
            if (this.Character != null && this.Character.OnAttackCharge(score))
                this.PlayerHit?.Invoke(score);
        }

        private void OnAttackTeam(InputAction.CallbackContext context)
        {
            if (context.performed && this.Character != null)
                this.Character.OnAttackTeam();
        }

        private void OnBlockPressed(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            var score = ScoreSystem.GetScore(BeatSystem.CurrentTrackTime, this);
            if (this.Character != null && this.Character.OnBlockPressed(score))
                this.PlayerHit?.Invoke(score);
        }

        private void OnBlockReleased(InputAction.CallbackContext context)
        {
            if (!context.canceled)
                return;

            var score = ScoreSystem.GetScore(BeatSystem.CurrentTrackTime, this);
            if (this.Character != null && this.Character.OnBlockReleased(score))
                this.PlayerHit?.Invoke(score);
        }

        private void OnPause(InputAction.CallbackContext context)
        {
            if (context.performed)
                GameManager.PauseGame(this);
        }

        private void UnregisterCharacterCallbacks(InputActionAsset actionMaps)
        {
            // Player map.
            var map = actionMaps.FindActionMap("Player", throwIfNotFound: true);

            var moveAction = map.FindAction("Move", throwIfNotFound: true);
            var attackLightAction = map.FindAction("Attack/Light", throwIfNotFound: true);
            var attackHeavyAction = map.FindAction("Attack/Heavy", throwIfNotFound: true);
            var attackTeamAction = map.FindAction("Attack/Team", throwIfNotFound: true);
            var blockAction = map.FindAction("Block", throwIfNotFound: true);
            var pauseAction = map.FindAction("Pause", throwIfNotFound: true);

            moveAction.performed -= this.OnMove;
            moveAction.canceled -= this.OnMove;
            attackLightAction.performed -= this.OnAttackLight;
            attackHeavyAction.performed -= this.OnAttackCharge;
            attackHeavyAction.canceled -= this.OnAttackHeavy;
            attackTeamAction.performed -= this.OnAttackTeam;
            blockAction.performed -= this.OnBlockPressed;
            blockAction.canceled -= this.OnBlockReleased;
            pauseAction.performed -= this.OnPause;
        }

        #endregion
    }
}
