using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class SelectionCircle : UIPanel
    {
        [Header("Beat Pulse")]
        [SerializeField] private Vector3 punchStrength = new Vector3(0.12f, 0.12f, 0.0f);
        [SerializeField] private float punchDuration = 0.28f;
        [SerializeField, Min(1)] private int punchVibrato = 6;
        [SerializeField, Range(0.0f, 1.0f)] private float punchElasticity = 0.7f;

        [Header("Colors")]
        [SerializeField] private Color fallbackColor = Color.white;
        [SerializeField, Range(0.0f, 1.0f)] private float haloAlpha = 0.12f;
        [SerializeField, Range(0.0f, 1.0f)] private float fillAlpha = 0.18f;

        protected override bool IsWorldSpace => true;

        private VisualElement halo;
        private VisualElement outerRing;
        private VisualElement innerRing;
        private Tween punchTween;
        private Vector3 baseScale;

        public override void OnInitialize()
        {
            this.halo = this.root.Q<VisualElement>("c_Halo");
            this.outerRing = this.root.Q<VisualElement>("c_OuterRing");
            this.innerRing = this.root.Q<VisualElement>("c_InnerRing");
            this.baseScale = this.transform.localScale;
        }

        public override void OnShow()
        {
            BeatSystem.BeatPlayed += this.OnBeatPlayed;
        }

        public override void OnHide()
        {
            BeatSystem.BeatPlayed -= this.OnBeatPlayed;
            this.ResetPulse();
        }

        private void OnDestroy()
        {
            BeatSystem.BeatPlayed -= this.OnBeatPlayed;
            this.ResetPulse();
        }

        public void ApplyColorway(Colorway colorway)
        {
            this.ApplyColor(colorway != null ? colorway.PrimaryColor : this.fallbackColor);
        }

        public void ApplyColor(Color color)
        {
            Color borderColor = color;
            borderColor.a = 1.0f;

            this.halo.style.backgroundColor = WithAlpha(color, this.haloAlpha);
            this.outerRing.style.backgroundColor = WithAlpha(color, this.fillAlpha);
            SetBorderColor(this.outerRing, borderColor);
            SetBorderColor(this.innerRing, borderColor);
        }

        private void OnBeatPlayed()
        {
            this.ResetPulse();
            this.punchTween = this.transform.DOPunchScale(this.punchStrength, this.punchDuration, this.punchVibrato, this.punchElasticity)
                .OnComplete(() => this.transform.localScale = this.baseScale);
        }

        private void ResetPulse()
        {
            this.punchTween?.Kill();
            this.punchTween = null;
            this.transform.localScale = this.baseScale;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static void SetBorderColor(VisualElement element, Color color)
        {
            element.style.borderTopColor = color;
            element.style.borderRightColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
        }
    }
}
