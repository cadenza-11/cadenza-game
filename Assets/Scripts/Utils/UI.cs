using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Cadenza.Utils
{
    public static class UI
    {
        /// <summary>
        /// Updates all input hints that are a child of this VisualElement
        /// to use image icons associated with a given controller's buttons.
        /// </summary>
        public static void UpdateInputHintsInHierarchy(this VisualElement element, ControllerType controller)
        {
            var inputHints = element.Query<InputHint>().Build();
            foreach (var inputHint in inputHints)
                inputHint.ShowForControllerType(controller);
        }

        /// <summary>
        /// Converts a movement vector into a discrete cardinal direction,
        /// or a "None" direction if the movement length is not beyond a threshold.
        /// </summary>
        public static MoveDirection GetMoveDirection(Vector2 move, float minMagnitude = 0.01f)
        {
            if (move.sqrMagnitude < minMagnitude)
                return MoveDirection.None;

            if (Mathf.Abs(move.x) > Mathf.Abs(move.y))
                return move.x > 0 ? MoveDirection.Right : MoveDirection.Left;
            else
                return move.y > 0 ? MoveDirection.Up : MoveDirection.Down;
        }
    }
}
