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
        public string Name => this.name;
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
                this.UnregisterCharacterCallbacks(this.Input.actions, this.Character);
        }

        #region Public Methods

        internal void Initialize(int id, PlayerInput input)
        {
            this.ID = id;
            this.Input = input;
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
                this.UnregisterCharacterCallbacks(this.Input.actions, this.Character);
                Destroy(this.Character);
            }

            this.Character = character;

            // Give input to new character body.
            if (this.Character != null)
            {
                this.Character.Initialize(this);
                this.RegisterCharacterCallbacks(this.Input.actions, this.Character);
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

        #endregion
        #region Input
        private void RegisterCharacterCallbacks(InputActionAsset actionMaps, CadenzaActions.IPlayerActions character)
        {
            // Player map.
            var map = actionMaps.FindActionMap("Player", throwIfNotFound: true);

            var moveAction = map.FindAction("Move", throwIfNotFound: true);
            var attackLightAction = map.FindAction("Attack/Light", throwIfNotFound: true);
            var attackHeavyAction = map.FindAction("Attack/Heavy", throwIfNotFound: true);
            var attackSpecialAction = map.FindAction("Attack/Special", throwIfNotFound: true);
            var attackTeamAction = map.FindAction("Attack/Team", throwIfNotFound: true);
            var pauseAction = map.FindAction("Pause", throwIfNotFound: true);
            this.interactAction = attackLightAction;

            moveAction.performed += character.OnMove;
            moveAction.canceled += character.OnMove;
            attackLightAction.performed += character.OnAttackLight;
            attackLightAction.performed += this.OnHit;
            attackHeavyAction.performed += character.OnAttackHeavy;
            attackHeavyAction.performed += this.OnHit;
            attackSpecialAction.performed += character.OnAttackSpecial;
            attackTeamAction.performed += character.OnAttackTeam;
            pauseAction.performed += this.OnPause;

            // UI map.
            map = actionMaps.FindActionMap("UI", throwIfNotFound: true);
            var unpauseAction = map.FindAction("Unpause", throwIfNotFound: true);
            unpauseAction.performed += this.OnUnPause;
        }

        private void OnPause(InputAction.CallbackContext context)
        {
            if (context.performed)
                GameManager.PauseGame(this);
        }

        private void OnUnPause(InputAction.CallbackContext context)
        {
            if (context.performed)
                GameManager.UnpauseGame();
        }

        private void OnHit(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                var score = ScoreSystem.GetScore(BeatSystem.CurrentTrackTime, this);
                this.PlayerHit?.Invoke(score);
            }
        }

        private void UnregisterCharacterCallbacks(InputActionAsset actionMaps, CadenzaActions.IPlayerActions character)
        {
            // Player map.
            var map = actionMaps.FindActionMap("Player", throwIfNotFound: true);

            var moveAction = map.FindAction("Move", throwIfNotFound: true);
            var attackLightAction = map.FindAction("Attack/Light", throwIfNotFound: true);
            var attackHeavyAction = map.FindAction("Attack/Heavy", throwIfNotFound: true);
            var attackSpecialAction = map.FindAction("Attack/Special", throwIfNotFound: true);
            var attackTeamAction = map.FindAction("Attack/Team", throwIfNotFound: true);

            moveAction.performed -= character.OnMove;
            moveAction.canceled -= character.OnMove;
            attackLightAction.performed -= character.OnAttackLight;
            attackHeavyAction.performed -= character.OnAttackHeavy;
            attackSpecialAction.performed -= character.OnAttackSpecial;
            attackTeamAction.performed -= character.OnAttackTeam;

            // UI map.
        }

        #endregion
    }
}
