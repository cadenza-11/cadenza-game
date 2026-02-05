using UnityEngine;

namespace Cadenza
{
    public class UISounds
    {
        public void Initialize()
        {
            InputSystem.UIPlayerCancel += this.OnUICancel;
            InputSystem.UIPlayerSubmit += this.OnUISubmit;
            InputSystem.UIPlayerNavigate += this.OnUINavigate;
        }

        private void OnUINavigate(Vector2 vector, Player player)
        {
            if (vector != Vector2.zero)
                AudioSystem.PlayOneShot(Sound.UI.NavMove, immediate: true);
        }

        private void OnUISubmit(Player player)
        {
            AudioSystem.PlayOneShot(Sound.UI.NavSubmit, immediate: true);
        }

        private void OnUICancel(Player player)
        {
            AudioSystem.PlayOneShot(Sound.UI.NavBack, immediate: true);
        }

        public void OnGameStart()
        {
        }

        public void OnGameStop()
        {
        }
    }
}
