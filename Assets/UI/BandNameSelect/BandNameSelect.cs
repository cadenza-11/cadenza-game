using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class BandNameSelect : UIPanel
    {
        protected override VisualElement InitialFocus => this.keyboard;

        [SerializeField] private TextAsset articlesFile;
        [SerializeField] private TextAsset adjectivesFile;
        [SerializeField] private TextAsset nounsFile;

        private OnScreenKeyboard keyboard;
        private string[] articles;
        private string[] adjectives;
        private string[] nouns;

        #region System Events
        public override void OnInitialize()
        {
            // Grab elements.
            this.keyboard = this.root.Q<OnScreenKeyboard>();
            this.Hide();

            // Override the 'cancel' button to be 'randomize'.
            this.keyboard.CancelButton.text = "Randomize";
            this.keyboard.CancelButton.clicked += this.OnRandomizeName;
            this.keyboard.SubmitButton.clicked += this.OnSetName;

            // Get wordlists
            this.articles = this.articlesFile.text.Split('\n');
            this.adjectives = this.adjectivesFile.text.Split('\n');
            this.nouns = this.nounsFile.text.Split('\n');
        }

        public override void OnShow()
        {
            InputSystem.UIPlayerSubmit += this.OnSubmit;
            InputSystem.UIPlayerCancel += this.OnCancel;
            InputSystem.UIPlayerNavigate += this.OnUIPlayerNavigate;
        }

        public override void OnHide()
        {
            InputSystem.UIPlayerSubmit -= this.OnSubmit;
            InputSystem.UIPlayerCancel -= this.OnCancel;
            InputSystem.UIPlayerNavigate -= this.OnUIPlayerNavigate;
        }

        private void OnSubmit(Player player)
        {
            this.keyboard.OnSubmit();
        }

        private void OnCancel(Player player)
        {
            this.keyboard.OnCancel();
        }
        private void OnUIPlayerNavigate(MoveDirection direction, Player player)
        {
            this.keyboard.OnNavigate(direction);
        }

        #endregion

        #region Private Functions

        private void OnRandomizeName()
        {
            string bandName = "";
            int oneIndex = Random.Range(-1, this.articles.Length);
            int threeIndex = Random.Range(-1, this.nouns.Length);
            bandName += oneIndex == -1 ? "" : (this.articles[oneIndex] + " ");
            bandName += this.adjectives[Random.Range(0, this.adjectives.Length)];
            bandName += threeIndex == -1 ? "" : (" " + this.nouns[threeIndex]);
            bandName = bandName.Replace("\r", "");
            this.keyboard.value = bandName;
        }

        private void OnSetName()
        {
            if (ApplicationController.IsRedirecting)
                return;

            // Redirect will trigger fade in; don't hide panel until faded in.
            this.Schedule(0.5f, () => this.Hide());

            // Prevent empty names.
            if (this.keyboard.value == string.Empty)
                this.OnRandomizeName();

            TeamSystem.CreateTeam(this.keyboard.value);
            GameManager.RedirectToBackstage();
        }

        #endregion
    }
}
