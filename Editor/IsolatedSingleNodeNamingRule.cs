using System;
using System.Collections.Generic;
using System.Linq;
using AAGen.Shared;
using UnityEngine;

namespace AAGen
{
    [CreateAssetMenu(menuName = Constants.ContextMenus.OutputRulesMenu + nameof(IsolatedSingleNodeNamingRule))]
    public class IsolatedSingleNodeNamingRule : NamingRule
    {
        protected override string CalculateName(SubgraphInfo subgraph)
        {
            var node = subgraph.Nodes.ToList()[0];
            return $"Isolated Node {node.FileName}";
        }
    }
}