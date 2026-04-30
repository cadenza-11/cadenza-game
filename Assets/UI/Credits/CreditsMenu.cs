using UnityEngine.UIElements;

namespace Cadenza
{
    public class CreditsMenu : UIPanel
    {
        private Button backButton;

        #region System Events
        public override void OnInitialize()
        {
            // Configure back button.
            this.backButton = this.root.Q<Button>("b_Back");
            this.backButton.clicked += () => this.TransitionTo(this.previousPanel);
            this.root.RegisterCallback<NavigationCancelEvent>(_ => this.TransitionTo(this.previousPanel));
        }

        public override void OnShow()
        {
            this.backButton.Focus();
        }

        #endregion
    }
}