using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace Cadenza
{
    public class BeatIndicator : UIPanel
    {
        private const int BeatsToCompletion = 4;
        private const string ArrowRightClass = "arrow-right";
        private const string ArrowLeftClass = "arrow-left";

        private VisualElement indicator;
        private VisualElement centerMarker;
        private readonly List<ArrowInstance> arrows = new();

        private float indicatorWidth;

        private class ArrowInstance
        {
            public VisualElement element;
            public bool fromLeft;
            public float duration;
            public float elapsed;
        }

        public override void OnInitialize()
        {
            this.Hide();

            this.indicator = this.root.Q<VisualElement>("c_BeatIndicator");
            this.centerMarker = this.root.Q<VisualElement>("c_Center");

            this.indicator?.RegisterCallback<GeometryChangedEvent>(this.OnGeometryChanged);
        }

        public override void OnGameStart()
        {
            this.Show();
        }

        public override void OnGameStop()
        {
            this.Hide();
        }

        public override void OnShow()
        {
            BeatSystem.BeatPlayed += this.OnBeatPlayed;
        }

        public override void OnHide()
        {
            BeatSystem.BeatPlayed -= this.OnBeatPlayed;
            this.ClearArrows();
        }

        public override void OnUpdate()
        {
            if (!this.IsVisible)
                return;

            if (this.arrows.Count == 0)
                return;

            // Update arrow positions.
            float deltaTime = Time.deltaTime;
            for (int i = this.arrows.Count - 1; i >= 0; i--)
            {
                var arrow = this.arrows[i];
                arrow.elapsed += deltaTime;
                float t = (arrow.duration > 0f) ? arrow.elapsed / arrow.duration : 1f;
                if (t >= 1f)
                {
                    // Remove arrow after it passes the middle.
                    arrow.element.RemoveFromHierarchy();
                    this.arrows.RemoveAt(i);
                    continue;
                }
                this.UpdateArrowPosition(arrow, t);
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (this.indicator == null)
                return;

            this.indicatorWidth = this.indicator.resolvedStyle.width;
        }

        private void OnBeatPlayed()
        {
            this.Pulse(this.centerMarker, Color.white, Color.orange, 1.0f, 1.2f, 100);
            this.SpawnArrowPair(durationOverride: (float)BeatSystem.SecondsPerBeat * BeatsToCompletion, initialElapsed: 0f);
        }

        private void SpawnArrowPair(float durationOverride, float initialElapsed)
        {
            if (this.indicator == null)
                return;

            float duration = durationOverride > 0f ? durationOverride : 0.1f;
            if (initialElapsed >= duration)
                return;

            this.CreateArrow(fromLeft: true, duration, initialElapsed);
            this.CreateArrow(fromLeft: false, duration, initialElapsed);
        }

        private void CreateArrow(bool fromLeft, float duration, float initialElapsed)
        {
            var arrowElement = new VisualElement();
            arrowElement.visible = false;
            {
                arrowElement.AddToClassList(fromLeft ? ArrowRightClass : ArrowLeftClass);
                arrowElement.style.position = Position.Absolute;
                this.indicator.Add(arrowElement);

                var arrow = new ArrowInstance
                {
                    element = arrowElement,
                    fromLeft = fromLeft,
                    duration = duration,
                    elapsed = initialElapsed,
                };
                this.arrows.Add(arrow);

                if (duration > 0f)
                    this.UpdateArrowPosition(arrow, Mathf.Clamp01(initialElapsed / duration));
            }
            arrowElement.visible = true;
        }

        private void UpdateArrowPosition(ArrowInstance arrow, float t)
        {
            float arrowWidth = arrow.element.resolvedStyle.width;

            float centerX = this.indicatorWidth * 0.5f;
            float endX = centerX - (arrowWidth * 0.5f);
            float startX = arrow.fromLeft ? -arrowWidth : this.indicatorWidth;
            float x = Mathf.Lerp(startX, endX, t);

            arrow.element.style.left = x;
        }

        private void ClearArrows()
        {
            for (int i = this.arrows.Count - 1; i >= 0; i--)
                this.arrows[i].element.RemoveFromHierarchy();
            this.arrows.Clear();
        }

        private void Pulse(
            VisualElement element,
            Color fromColor,
            Color toColor,
            float fromScale,
            float toScale,
            int durationMs)
        {
            // Color animation
            element.experimental.animation
                .Start(
                    fromColor,
                    toColor,
                    durationMs,
                    (e, value) =>
                    {
                        e.style.unityBackgroundImageTintColor = value;
                    }
                )
                .Ease(Easing.InOutSine)
                .OnCompleted(() =>
                {
                    element.experimental.animation
                        .Start(
                            toColor,
                            fromColor,
                            durationMs,
                            (e, value) =>
                            {
                                e.style.unityBackgroundImageTintColor = value;
                            }
                        )
                        .Ease(Easing.InOutSine);
                });

            // Scale animation
            element.experimental.animation
                .Start(
                    fromScale,
                    toScale,
                    durationMs,
                    (e, value) =>
                    {
                        e.style.scale = new Scale(new Vector3(value, value, 1f));
                    }
                )
                .Ease(Easing.InOutSine)
                .OnCompleted(() =>
                {
                    element.experimental.animation
                        .Start(
                            toScale,
                            fromScale,
                            durationMs,
                            (e, value) =>
                            {
                                e.style.scale = new Scale(new Vector3(value, value, 1f));
                            }
                        )
                        .Ease(Easing.InOutSine);
                });
        }
    }
}
