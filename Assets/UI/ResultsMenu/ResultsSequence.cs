using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class ResultsSequence
    {
        private static readonly Color IntroStartColor = new(178f / 255f, 178f / 255f, 178f / 255f);
        private readonly object introTweenTarget = new();
        private readonly JudgeResultsIntro judgeResultsIntro = new();

        private float tweenDuration;
        private VisualElement resultsPanel;
        private VisualElement actionButtonsContainer;

        public bool IsPlaying { get; private set; }

        private static string lastMarkerName;
        private EventReference resultsMusicEvent;
        private EventInstance instance;
        private GCHandle handle;

        private class JudgeResultsIntro
        {
            public VisualElement Container;
            public VisualElement JudgeScoresContainer;
            public VisualElement OverallResultsContainer;
            public VisualElement OverallScoreContainer;
            public VisualElement OverallGradeContainer;
            public VisualElement[] JudgeScoreContainers;
            public Label[] JudgeScoreLabels;
            public Label OverallScoreLabel;
            public Label OverallGradeLabel;
        }

        #region FMOD Callbacks

        [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
        private static FMOD.RESULT MarkerEventCallback(EVENT_CALLBACK_TYPE type, IntPtr _, IntPtr parameterPtr)
        {
            // Get marker info.
            if (type == EVENT_CALLBACK_TYPE.TIMELINE_MARKER)
            {
                var parameter = (TIMELINE_MARKER_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(TIMELINE_MARKER_PROPERTIES));
                OnMarker(parameter.name);
            }

            return FMOD.RESULT.OK;
        }

        public static void OnMarker(string markerName)
        {
            lastMarkerName = markerName;
        }

        #endregion
        #region Application Callbacks

        public void OnInitialize(
            VisualElement root,
            VisualElement resultsPanel,
            VisualElement actionButtonsContainer,
            EventReference resultsMusicEvent,
            float tweenDuration)
        {
            this.tweenDuration = tweenDuration;
            this.resultsMusicEvent = resultsMusicEvent;
            this.resultsPanel = resultsPanel;
            this.actionButtonsContainer = actionButtonsContainer;

            var judgeResultsContainers = root.Query<VisualElement>("c_JudgeResults").ToList();
            this.judgeResultsIntro.Container = judgeResultsContainers.Count > 0 ? judgeResultsContainers[0] : null;
            this.judgeResultsIntro.JudgeScoresContainer = judgeResultsContainers.Count > 1 ? judgeResultsContainers[1] : null;
            this.judgeResultsIntro.OverallResultsContainer = root.Q<VisualElement>("c_OverallResults");
            this.judgeResultsIntro.OverallScoreContainer = root.Q<VisualElement>("score_Overall");
            this.judgeResultsIntro.OverallGradeContainer = root.Q<VisualElement>("score_Grade");
            this.judgeResultsIntro.JudgeScoreContainers = new[]
            {
                root.Q<VisualElement>("score_Judge1"),
                root.Q<VisualElement>("score_Judge2"),
                root.Q<VisualElement>("score_Judge3"),
            };
            this.judgeResultsIntro.JudgeScoreLabels = new[]
            {
                root.Q<Label>("txt_judge1"),
                root.Q<Label>("txt_judge2"),
                root.Q<Label>("txt_judge3"),
            };
            this.judgeResultsIntro.OverallScoreLabel = root.Q<Label>("txt_Overall");
            this.judgeResultsIntro.OverallGradeLabel = root.Q<Label>("txt_Grade");


            this.ResetJudgeResults();
        }

        public void OnApplicationStop()
        {
            this.instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            this.instance.release();

            if (this.handle.IsAllocated)
                this.handle.Free();
        }

        public void OnShow()
        {
        }

        public void OnHide()
        {
            this.CancelAndReset();
        }

        public void OnGameStart()
        {
            this.CancelAndReset();
        }

        public void OnGameStop()
        {
            this.CancelAndReset();
        }

        #endregion
        #region Public Methods

        public void Play(Results results)
        {
            if (results == null || this.judgeResultsIntro.Container == null || this.IsPlaying)
                return;

            // Reset animation.
            this.IsPlaying = true;
            DOTween.Kill(this.introTweenTarget);
            this.ResetJudgeResults();

            _ = this.PlayResultsIntroAsync(results);
        }

        #endregion
        #region Private Methods

        private async Task PlayResultsIntroAsync(Results results)
        {
            // Wait for any audio transitions to resolve.
            await BeatSystem.WaitForMarkerAsync("State_Postcombat");

            // Reset UI.
            this.judgeResultsIntro.Container.style.display = DisplayStyle.Flex;

            for (int i = 0; i < this.judgeResultsIntro.JudgeScoreLabels.Length; i++)
                this.judgeResultsIntro.JudgeScoreLabels[i].text = results.JudgeScores[i].ToString("F0");

            this.judgeResultsIntro.OverallScoreLabel.text = results.OverallScore.ToString("F0");
            this.judgeResultsIntro.OverallGradeLabel.text = ScoreSystem.GetGradeLetter(results.OverallScore);

            // Setup marker callbacks for results audio.
            this.instance = RuntimeManager.CreateInstance(this.resultsMusicEvent);
            this.handle = GCHandle.Alloc(this);
            this.instance.setUserData(GCHandle.ToIntPtr(this.handle));
            this.instance.setCallback(MarkerEventCallback, EVENT_CALLBACK_TYPE.TIMELINE_MARKER);
            this.instance.setParameterByName("ResultsGrade", ScoreSystem.GetGradeLetter(results.OverallScore) switch
            {
                "S" => 5,
                "A" => 4,
                "B" => 3,
                "C" => 2,
                "D" => 1,
                _ => 0,
            });

            // Play results audio.
            this.instance.start();

            // Prepare judge results.
            this.judgeResultsIntro.Container.style.display = DisplayStyle.Flex;
            this.judgeResultsIntro.JudgeScoresContainer.style.display = DisplayStyle.Flex;
            this.judgeResultsIntro.OverallResultsContainer.style.display = DisplayStyle.None;

            for (int i = 0; i < this.judgeResultsIntro.JudgeScoreContainers.Length; i++)
                this.PrepareScoreAnimation(this.judgeResultsIntro.JudgeScoreContainers[i], this.judgeResultsIntro.JudgeScoreLabels[i]);

            // Animate judge results.
            for (int i = 0; i < this.judgeResultsIntro.JudgeScoreContainers.Length; i++)
            {
                await this.WaitForMarkerAsync("Result_Score" + (i + 1));
                await this.AnimateScoreAsync(
                    this.judgeResultsIntro.JudgeScoreContainers[i],
                    this.judgeResultsIntro.JudgeScoreLabels[i],
                    results.JudgeScores[i],
                    this.tweenDuration);
            }

            await this.WaitForMarkerAsync("Result_Overall");

            // Prepare overall score.
            this.judgeResultsIntro.JudgeScoresContainer.style.display = DisplayStyle.None;
            this.judgeResultsIntro.OverallResultsContainer.style.display = DisplayStyle.Flex;
            this.PrepareScoreAnimation(this.judgeResultsIntro.OverallScoreContainer, this.judgeResultsIntro.OverallScoreLabel);
            this.PrepareScoreAnimation(this.judgeResultsIntro.OverallGradeContainer, this.judgeResultsIntro.OverallGradeLabel);

            // Animate overall score and grade.
            await Task.WhenAll(
                this.AnimateScoreAsync(
                    this.judgeResultsIntro.OverallScoreContainer,
                    this.judgeResultsIntro.OverallScoreLabel,
                    results.OverallScore,
                    this.tweenDuration),
                this.AnimateScoreAsync(
                    this.judgeResultsIntro.OverallGradeContainer,
                    this.judgeResultsIntro.OverallGradeLabel,
                    results.OverallScore,
                    this.tweenDuration));

            await this.WaitForMarkerAsync("Result_End");

            // Resume level-based results audio.
            AudioSystem.SetState(AudioSystem.State.Results);
            await BeatSystem.WaitForMarkerAsync("State_Results");

            // Prepare results panel.
            this.judgeResultsIntro.Container.style.display = DisplayStyle.None;
            this.resultsPanel.style.display = DisplayStyle.Flex;
            this.SetScale(this.resultsPanel, 0f);

            // Animate results panel intro.
            float panelScale = 0f;
            Tween panelTween = DOTween.To(
                    () => panelScale,
                    value =>
                    {
                        panelScale = value;
                        this.SetScale(this.resultsPanel, value);
                    },
                    1f,
                    this.tweenDuration)
                .SetEase(Ease.OutBack)
                .SetTarget(this.introTweenTarget);

            await panelTween.AsyncWaitForCompletion();
            this.IsPlaying = false;
        }

        private async Task WaitForMarkerAsync(string markerName)
        {
            while (lastMarkerName != markerName)
                await Task.Yield();
        }

        private async Task AnimateScoreAsync(VisualElement container, Label label, float score, float duration)
        {
            float scale = 0f;
            Color color = IntroStartColor;
            Color targetColor = this.GetScoreColor(score);

            Sequence sequence = DOTween.Sequence();
            sequence.SetTarget(this.introTweenTarget);

            sequence.Join(
                DOTween.To(
                        () => scale,
                        value =>
                        {
                            scale = value;
                            this.SetScale(label, value);
                        },
                        1f,
                        duration)
                    .SetEase(Ease.InOutElastic)
            );
            sequence.Join(
                DOTween.To(
                        () => color,
                        value =>
                        {
                            color = value;
                            container.style.backgroundColor = value;
                        },
                        targetColor,
                        duration)
                    .SetEase(Ease.InOutElastic)
            );

            await sequence.AsyncWaitForCompletion();
        }

        private void CancelAndReset()
        {
            this.IsPlaying = false;
            DOTween.Kill(this.introTweenTarget);
            this.ResetJudgeResults();
        }

        private void PrepareScoreAnimation(VisualElement container, Label label)
        {
            container.style.backgroundColor = IntroStartColor;
            this.SetScale(label, 0f);
        }

        private Color GetScoreColor(float score)
        {
            return Color.Lerp(Color.red, Color.green, Mathf.Clamp01(score / ScoreSystem.MaxScore));
        }

        private void SetScale(VisualElement element, float scale)
        {
            element.style.scale = new Scale(Vector3.one * scale);
        }

        private void ResetJudgeResults()
        {
            if (this.judgeResultsIntro.Container == null || this.resultsPanel == null || this.actionButtonsContainer == null)
                return;

            this.judgeResultsIntro.Container.style.display = DisplayStyle.None;
            this.judgeResultsIntro.JudgeScoresContainer.style.display = DisplayStyle.Flex;
            this.judgeResultsIntro.OverallResultsContainer.style.display = DisplayStyle.None;

            for (int i = 0; i < this.judgeResultsIntro.JudgeScoreContainers.Length; i++)
            {
                this.judgeResultsIntro.JudgeScoreContainers[i].style.backgroundColor = IntroStartColor;
                this.judgeResultsIntro.JudgeScoreLabels[i].text = string.Empty;
                this.SetScale(this.judgeResultsIntro.JudgeScoreLabels[i], 1f);
            }

            this.judgeResultsIntro.OverallScoreContainer.style.backgroundColor = IntroStartColor;
            this.judgeResultsIntro.OverallScoreLabel.text = string.Empty;
            this.SetScale(this.judgeResultsIntro.OverallScoreLabel, 1f);
            this.judgeResultsIntro.OverallGradeContainer.style.backgroundColor = IntroStartColor;
            this.judgeResultsIntro.OverallGradeLabel.text = string.Empty;
            this.SetScale(this.judgeResultsIntro.OverallGradeLabel, 1f);
            this.SetScale(this.resultsPanel, 1f);
            this.resultsPanel.style.display = DisplayStyle.None;
            this.actionButtonsContainer.style.display = DisplayStyle.None;
        }

        #endregion
    }
}
