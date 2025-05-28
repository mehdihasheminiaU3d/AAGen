using AAGen.Shared;
using UnityEngine;

namespace AAGen
{
    [CreateAssetMenu(menuName = Constants.ContextMenus.RefinementRulesMenu + nameof(SelectAllSubgraphs))]
    public class SelectAllSubgraphs : SubgraphSelector
    {
        protected override bool IsMatch(SubgraphInfo subgraph) => true;
    }
}