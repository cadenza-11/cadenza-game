using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class BandNameSelect : UIPanel
    {

        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private TextAsset articlesFile;
        [SerializeField] private TextAsset adjectivesFile;
        [SerializeField] private TextAsset nounsFile;

        private Button randomizeName;
        private Button setName;
        private TextField bandNameField;
        private string[] articles;
        private string[] adjectives;
        private string[] nouns;

        #region System Events
        public override void OnInitialize()
        {
            // Set up UI.
            this.root = (TemplateContainer)this.uiDocument.rootVisualElement;
            this.root.style.display = DisplayStyle.None;

            // Grab elements.
            this.randomizeName = this.root.Q<Button>("b_RandomName");
            this.randomizeName.clicked += this.OnRandomizeName;
            this.setName = this.root.Q<Button>("b_SetName");
            this.setName.clicked += this.OnSetName;
            this.bandNameField = this.root.Q<TextField>("field_BandName");
            this.Hide();

            // Get wordlists
            this.articles = this.articlesFile.text.Split('\n');
            this.adjectives = this.adjectivesFile.text.Split('\n');
            this.nouns = this.nounsFile.text.Split('\n');
        }

        public override void Show()
        {
            base.Show();
            PlayerSystem.TryGetPlayerByID(0, out Player player);
            // if (player != null)
            //     InputSystem.EnableSinglePlayerInput(player);
            this.root.style.display = DisplayStyle.Flex;
        }

        public override void Hide()
        {
            base.Hide();
            this.root.style.display = DisplayStyle.None;
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
            Debug.Log($"Generated name: {bandName}");
            this.bandNameField.value = bandName;
        }

        private void OnSetName()
        {
            TeamSystem.CreateTeam(this.bandNameField.value);
            _ = ApplicationController.SetSceneAsync(1);
            this.Hide();
        }

        #endregion
    }
}
