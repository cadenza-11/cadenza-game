using UnityEngine;

namespace Cadenza
{
    /// <summary>
    /// Handles enabling and disabling of input actions, input action maps, and player input.
    /// </summary>
    public class UISystem : ApplicationSystem
    {
        private static UISystem singleton;
        private UIPanel[] panels;

        public override void OnInitialize()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            this.panels = this.GetComponentsInChildren<UIPanel>();

            foreach (var panel in this.panels)
                panel.Initialize();
        }

        public override void OnGameStart()
        {
            foreach (var panel in this.panels)
                panel.OnGameStart();
        }

        public override void OnGameStop()
        {
            foreach (var panel in this.panels)
                panel.OnGameStop();
        }

        public override void OnApplicationStop()
        {
            foreach (var panel in this.panels)
                panel.OnApplicationStop();
        }

        public override void OnUpdate()
        {
            foreach (var panel in this.panels)
                panel.OnUpdate();
        }

        public static T FindPanel<T>() where T : UIPanel
        {
            foreach (var panel in singleton.panels)
            {
                if (panel is T foundPanel)
                    return foundPanel;
            }
            return null;
        }
    }
}
