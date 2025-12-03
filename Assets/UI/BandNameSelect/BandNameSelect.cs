using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class BandNameSelect : UIPanel
    {

        [SerializeField] private UIDocument uiDocument;

        private Button randomizeName;
        private Button setName;
        private TextField bandNameField;

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
        }

        public override void Show()
        {
            base.Show();
            PlayerSystem.TryGetPlayerByID(0, out Player player);
            if (player != null)
                InputSystem.EnableSinglePlayerInput(player);
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
            Debug.LogWarning("No wordlist implemented. Default name 'The Loud Pandas' used.");
            this.bandNameField.value = "The Loud Pandas";
        }

        private void OnSetName()
        {
            Debug.LogWarning($"Band name '{this.bandNameField.value}' not saved. No functionality.");
            _ = ApplicationController.SetSceneAsync(1);
            this.Hide();
        }

        #endregion
    }
}
