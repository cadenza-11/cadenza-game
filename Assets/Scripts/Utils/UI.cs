using System;
using DG.Tweening;
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

        public static string GetHumanizedTime(DateTime dateTime)
        {
            var ts = DateTime.UtcNow - dateTime.ToUniversalTime();

            if (ts.TotalMinutes < 1)
                return "just now";

            if (ts.TotalHours < 1)
                return $"{(int)ts.TotalMinutes} minute{Plural(ts.TotalMinutes)} ago";

            if (ts.TotalDays < 1)
                return $"{(int)ts.TotalHours} hour{Plural(ts.TotalHours)} ago";

            if (ts.TotalDays < 30)
                return $"{(int)ts.TotalDays} day{Plural(ts.TotalDays)} ago";

            if (ts.TotalDays < 365)
                return $"{(int)(ts.TotalDays / 30)} month{Plural(ts.TotalDays / 30)} ago";

            return $"{(int)(ts.TotalDays / 365)} year{Plural(ts.TotalDays / 365)} ago";
        }

        private static string Plural(double value)
        {
            return (int)value == 1 ? "" : "s";
        }

        public static void PunchAndShake(this VisualElement element, float duration)
        {
            Vector3 baseScale = Vector3.one;
            Vector3 baseOffset = Vector3.zero;

            Sequence seq = DOTween.Sequence();

            seq.Join(
                DOTween.Punch(
                    getter: () => baseScale,
                    setter: v => { element.style.scale = new Scale(v); },
                    direction: new Vector3(0.3f, 0.3f, 0),
                    duration: duration,
                    vibrato: 10,
                    elasticity: 1
                )
            );
            seq.Join(
                DOTween.Shake(
                    getter: () => baseOffset,
                    setter: v => { element.style.translate = new Translate(v.x, v.y, 0); },
                    duration: duration,
                    strength: new Vector3(8f, 8f, 0),
                    vibrato: 20,
                    randomness: 90
                )
            );

            seq.OnComplete(() =>
            {
                element.style.scale = new Scale(Vector3.one);
                element.style.translate = new Translate(0, 0, 0);
            });
        }
    }
}
