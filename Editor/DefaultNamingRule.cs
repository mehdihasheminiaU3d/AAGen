using System.Collections.Generic;
using AAGen.Shared;
using UnityEngine;

namespace AAGen
{
    [CreateAssetMenu(menuName = Constants.ContextMenus.RefinementRulesMenu + nameof(DefaultNamingRule))]
    public class DefaultNamingRule : NamingRule
    {
        protected override string CalculateName(SubgraphInfo subgraph)
        {
            return subgraph.HashOfSources.ToString();
        }
    }
}