using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza.Tests
{
    public class UISystemTests
    {
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;

        private GameObject testObject;
        private BandNameSelect bandNameSelect;

        [SetUp]
        public void SetUp()
        {
            this.testObject = new GameObject(nameof(UISystemTests));
            this.bandNameSelect = this.testObject.AddComponent<BandNameSelect>();

            // Create mock text assets
            var articles = new TextAsset("the\na\none\n");
            var adjectives = new TextAsset("epic\nswag\nrad\n");
            var nouns = new TextAsset("band\ngroup\ngang\n");
            
            SetInstanceField(this.bandNameSelect, "articlesFile", articles);
            SetInstanceField(this.bandNameSelect, "adjectivesFile", adjectives);
            SetInstanceField(this.bandNameSelect, "nounsFile", nouns);
            
            // Create root visual element hierarchy
            var uiDocument = this.testObject.AddComponent<UIDocument>();
            var keyboard = new OnScreenKeyboard();
            uiDocument.rootVisualElement.Add(keyboard);
            
            this.bandNameSelect.uiDocument = uiDocument;
        }

        [TearDown]
        public void TearDown()
        {
            if (this.testObject != null)
                Object.DestroyImmediate(this.testObject);
        }

        [Test]
        public void BandNameSelect_OnRandomizeName_GeneratesRandomTeamName()
        {
            // Initialize UI.
            this.bandNameSelect.Initialize();
            this.bandNameSelect.OnShow();
            
            // Simulate pressing the keyboard's cancel button (which calls OnRandomizeName).
            var bandNameSelectType = this.bandNameSelect.GetType();
            var onRandomizeNameMethod = bandNameSelectType.GetMethod("OnRandomizeName", InstancePrivate);
            onRandomizeNameMethod.Invoke(this.bandNameSelect, null);

            // Assert that the keyboard's value is not empty and a name was generated.
            var keyboard = GetInstanceField(this.bandNameSelect, "keyboard") as OnScreenKeyboard;
            TestContext.WriteLine($"Generated band name: '{keyboard.value}'");
            Assert.IsNotEmpty(keyboard.value);
            Assert.IsFalse(string.IsNullOrWhiteSpace(keyboard.value));
        }

        private static void SetInstanceField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, InstancePrivate);
            Assert.NotNull(field, $"Expected field '{fieldName}' to exist.");
            field.SetValue(target, value);
        }

        private static object GetInstanceField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, InstancePrivate);
            Assert.NotNull(field, $"Expected field '{fieldName}' to exist.");
            return field.GetValue(target);
        }
    }
}
