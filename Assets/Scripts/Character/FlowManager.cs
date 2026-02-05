using UnityEngine;

namespace Cadenza
{
    public class FlowManager : MonoBehaviour
    {
        private static FlowManager singleton;
        public static FlowManager Singleton => singleton;
        public bool[] playerFlows = { false, false, false, false };

        private void Awake()
        {
            if (singleton != null && singleton != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                singleton = this;
            }
        }
    }

}
