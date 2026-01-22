using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cadenza.Combo
{
    [Serializable]
    public struct ComboReward
    {
        public int Multiplier;
        public AttkEffect AttackEffect;
    }

    [CreateAssetMenu(fileName = "ComboDef", menuName = "Cadenza/Combos/ComboDef", order = 2)]
    public class ComboDefinition : ScriptableObject
    {

        public List<AttkTypes> Moves;
        public ComboReward Reward;

    }
}
