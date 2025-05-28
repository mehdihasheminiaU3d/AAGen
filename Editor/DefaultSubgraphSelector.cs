using AAGen.Shared;
using UnityEngine;

namespace AAGen
{
    [CreateAssetMenu(menuName = Constants.ContextMenus.RefinementRulesMenu + nameof(DefaultSubgraphSelector))]
    public class DefaultSubgraphSelector : SubgraphSelector
    {
        protected override bool IsMatch(SubgraphInfo subgraph) => true;
    }
}