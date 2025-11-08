using UnityEngine;
using UnityEngine.InputSystem;

namespace Cadenza
{
    public class Player : MonoBehaviour
    {
        #region Attributes

        public int ID { get; private set; }
        public Character Character { get; private set; }
        public PlayerInput Input { get; private set; }
        public string Name => this.name;
        public double Latency => ScoreSystem.GetInputLatencyForPlayer(this);

        #endregion
        #region Functions

        private void OnDestroy()
        {
            if (this.Character != null)
                this.UnregisterCharacterCallbacks(this.Input.actions, this.Character);
        }

        internal void Initialize(int id, PlayerInput input)
        {
            this.ID = id;
            this.Input = input;
        }

        /// <summary>
        /// Tracks this player to an instance of a character body.
        /// </summary>
        /// <param name="character">A spawned instance of the player body.</param>
        public void SetCharacter(Character character)
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
                this.RegisterCharacterCallbacks(this.Input.actions, this.Character);
            }

        }

        private void RegisterCharacterCallbacks(InputActionAsset actionMaps, CadenzaActions.IPlayerActions character)
        {
            var map = actionMaps.FindActionMap("Player", throwIfNotFound: true);

            var moveAction = map.FindAction("Move", throwIfNotFound: true);
            var attackLightAction = map.FindAction("Attack/Light", throwIfNotFound: true);
            var attackSpecialAction = map.FindAction("Attack/Special", throwIfNotFound: true);
            var attackTeamAction = map.FindAction("Attack/Team", throwIfNotFound: true);

            moveAction.performed += character.OnMove;
            moveAction.canceled += character.OnMove;
            attackLightAction.performed += character.OnAttackLight;
            attackSpecialAction.performed += character.OnAttackSpecial;
            attackTeamAction.performed += character.OnAttackTeam;
        }

        private void UnregisterCharacterCallbacks(InputActionAsset actionMaps, CadenzaActions.IPlayerActions character)
        {
            var map = actionMaps.FindActionMap("Player", throwIfNotFound: true);

            var moveAction = map.FindAction("Move", throwIfNotFound: true);
            var attackLightAction = map.FindAction("Attack/Light", throwIfNotFound: true);
            var attackSpecialAction = map.FindAction("Attack/Special", throwIfNotFound: true);
            var attackTeamAction = map.FindAction("Attack/Team", throwIfNotFound: true);

            moveAction.performed -= character.OnMove;
            moveAction.canceled -= character.OnMove;
            attackLightAction.performed -= character.OnAttackLight;
            attackSpecialAction.performed -= character.OnAttackSpecial;
            attackTeamAction.performed -= character.OnAttackTeam;
        }

        #endregion
    }
}
