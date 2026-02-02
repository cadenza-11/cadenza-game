using UnityEngine;

namespace Cadenza
{
    [CreateAssetMenu(fileName = "FlowManager", menuName = "Cadenza/FlowManager")]
    public class FlowManagerSO : ScriptableObject
    {
        public bool[] playerFlows = { false, false, false, false };
    }

}
