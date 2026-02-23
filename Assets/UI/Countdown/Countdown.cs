using UnityEngine;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class Countdown : UIPanel
    {
        public override void OnInitialize()
        {
            GameManager.CombatStarted += this.OnCombatStarted;
            GameManager.CombatStopped += this.OnCombatStopped;
        }

        private void OnCombatStarted()
        {
        }

        private void OnCombatStopped(GameManager.GameResult result)
        {
        }
    }
}
