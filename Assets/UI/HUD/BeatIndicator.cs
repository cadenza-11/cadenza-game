using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace Cadenza
{
    [UxmlElement]
    public partial class BeatIndicator : VisualElement
    {
        private const int DefaultBeatsToCompletion = 4;
        private const string RootClass = "beat-indicator-root";
        private const string IndicatorName = "c_BeatIndicator";
        private const string CenterName = "c_Center";
        private const string ArrowRightClass = "arrow-right";
        private const string ArrowLeftClass = "arrow-left";

        private readonly List<ArrowInstance> arrows = new();
        private VisualElement indicator;
        private VisualElement centerMarker;
        private float indicatorWidth;
        private bool isListening;

        private class ArrowInstance
        {
            public VisualElement element;
            public bool fromLeft;
            public float duration;
            public float elapsed;
        }

        public int BeatsToCompletion { get; private set; } = DefaultBeatsToCompletion;

        public BeatIndicator()
        {
            this.AddToClassList(RootClass);
            this.BuildVisualTree();
        }

        private void BuildVisualTree()
        {
            this.indicator = new VisualElement { name = IndicatorName };
            this.indicator.AddToClassList("beat-indicator");

            this.centerMarker = new VisualElement { name = CenterName };
            this.centerMarker.AddToClassList("center-marker");

            this.hierarchy.Add(this.indicator);
            this.hierarchy.Add(this.centerMarker);

            this.indicator.RegisterCallback<GeometryChangedEvent>(this.OnGeometryChanged);
        }

        /// <summary>
        /// Begin listening to the BeatSystem in order to pulse the UI.
        /// </summary>
        public void Start()
        {
            if (!this.isListening)
            {
                BeatSystem.BeatPlayed += this.OnBeatPlayed;
                this.isListening = true;
            }
        }

        /// <summary>
        /// Stop listening to the BeatSystem.
        /// </summary>
        public void Stop()
        {
            if (this.isListening)
            {
                BeatSystem.BeatPlayed -= this.OnBeatPlayed;
                this.isListening = false;
            }
            this.ClearArrows();
        }

        /// <summary>
        /// Updates positions of the arrows.
        /// </summary>
        public void Update()
        {
            if (this.arrows.Count == 0)
                return;

            if (this.indicator == null)
                return;

            float deltaTime = Time.unscaledDeltaTime;
            for (int i = this.arrows.Count - 1; i >= 0; i--)
            {
                var arrow = this.arrows[i];
                arrow.elapsed += deltaTime;
                float t = (arrow.duration > 0f) ? arrow.elapsed / arrow.duration : 1f;
                if (t >= 1f)
                {
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
            Debug.Log($"Beat played, spawning arrow pair. (beat={BeatSystem.GetClosestBeat(BeatSystem.CurrentTrackTime)})");
            if (this.centerMarker != null)
                this.Pulse(this.centerMarker, Color.white, new Color(1f, 0.5f, 0f, 1f), 1.0f, 1.2f, 100);

            float duration = (float)BeatSystem.SecondsPerBeat * this.BeatsToCompletion;
            this.SpawnArrowPair(durationOverride: duration, initialElapsed: 0f);
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
            if (this.indicator == null)
                return;

            var arrowElement = new VisualElement();
            arrowElement.visible = false;
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

            arrowElement.visible = true;
        }

        private void UpdateArrowPosition(ArrowInstance arrow, float t)
        {
            float currentWidth = this.indicatorWidth;
            if (currentWidth <= 0f && this.indicator != null)
                currentWidth = this.indicator.resolvedStyle.width;

            float arrowWidth = arrow.element.resolvedStyle.width;
            float centerX = currentWidth * 0.5f;
            float endX = centerX - (arrowWidth * 0.5f);
            float startX = arrow.fromLeft ? -arrowWidth : currentWidth;
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
