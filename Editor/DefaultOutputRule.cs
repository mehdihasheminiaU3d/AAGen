using AAGen.Shared;
using UnityEngine;

namespace AAGen
{
    [CreateAssetMenu(menuName = Constants.ContextMenus.OutputRulesMenu + nameof(DefaultOutputRule))]
    public class DefaultOutputRule : OutputRule
    {
        protected override bool DoesSubgraphMatchSelectionCriteria(SubgraphInfo subgraph)
        {
            return true;
        }

        protected override string CalculateName(SubgraphInfo subgraph)
        {
            return GetFallbackName(subgraph);
        }
    }
}