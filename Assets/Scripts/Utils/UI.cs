using UnityEngine;
using UnityEngine.EventSystems;

namespace Cadenza.Utils
{
    public static class UI
    {
        public static MoveDirection GetMoveDirection(Vector2 move)
        {
            if (move.sqrMagnitude < 0.01f)
                return MoveDirection.None;

            if (Mathf.Abs(move.x) > Mathf.Abs(move.y))
                return move.x > 0 ? MoveDirection.Right : MoveDirection.Left;
            else
                return move.y > 0 ? MoveDirection.Up : MoveDirection.Down;
        }
    }
}
