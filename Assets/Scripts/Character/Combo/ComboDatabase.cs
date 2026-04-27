using System.Collections.Generic;
using UnityEngine;

namespace Cadenza.Combo
{
    [CreateAssetMenu(fileName = "ComboDB", menuName = "Cadenza/Combos/ComboDB", order = 1)]
    public class ComboDatabase : ScriptableObject
    {
        public List<ComboDefinition> ComboDefinitions;

        public ComboNode BuildTree()
        {
            var root = new ComboNode();

            if (this.ComboDefinitions == null)
                return root;

            foreach (var combo in this.ComboDefinitions)
            {
                if (combo == null || combo.Moves == null)
                    continue;

                var current = root;
                foreach (var move in combo.Moves)
                {
                    if (!current.Children.TryGetValue(move, out var next))
                    {
                        next = new ComboNode();
                        current.Children[move] = next;
                    }

                    current = next;
                }

                current.IsEnd = true;
                // Can be comboDefinition instead if you want
                current.Reward = combo.Reward;
            }

            return root;
        }
    }

    public class ComboNode
    {
        public Dictionary<AttkTypes, ComboNode> Children = new();
        public bool IsEnd;
        public ComboReward Reward;
    }
}
