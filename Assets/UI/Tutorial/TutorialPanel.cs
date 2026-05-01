using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class TutorialPanel : UIPanel
    {
        private const float DurationSeconds = 10f;

        private List<VisualElement> pages = new();
        private int currentPageIndex;
        private Coroutine hideCoroutine;

        public override void OnInitialize()
        {
            this.pages = this.root.Query<VisualElement>(className: "tutorial-page").ToList();
            this.HidePages();
            this.Hide();
        }

        public override void OnGameStop()
        {
            this.Hide();
        }

        public override void OnShow()
        {
            this.ShowPage(0);
            this.ScheduleNextPage();
        }

        public override void OnHide()
        {
            this.HidePages();
        }

        public void NextPage()
        {
            if (this.IsVisible)
            {
                this.ShowPage((this.currentPageIndex + 1) % this.pages.Count);
                this.ScheduleNextPage();
                return;
            }

            this.Show();
        }

        private void ShowPage(int pageIndex)
        {
            if (this.pages.Count == 0)
                return;

            this.currentPageIndex = Mathf.Clamp(pageIndex, 0, this.pages.Count - 1);

            for (int i = 0; i < this.pages.Count; i++)
                this.pages[i].style.display = i == this.currentPageIndex ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void HidePages()
        {
            foreach (var page in this.pages)
                page.style.display = DisplayStyle.None;
        }

        private void ScheduleNextPage()
        {
            if (this.hideCoroutine != null)
                this.StopCoroutine(this.hideCoroutine);
            this.hideCoroutine = this.Schedule(DurationSeconds, this.Hide);
        }
    }
}
