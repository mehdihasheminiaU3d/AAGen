using System.Linq;
using AAGen.Shared;
using UnityEngine;

namespace AAGen
{
    [CreateAssetMenu(menuName = Constants.ContextMenus.RefinementRulesMenu + nameof(SingleIsolatedNodeSelector))]
    public class SingleIsolatedNodeSelector : SubgraphSelector
    {
        protected override bool IsMatch(SubgraphInfo subgraph)
        {
            return SubgraphTopologyUtil.IsSingleIsolatedNode(subgraph, m_DependencyGraph);
        }
    }
}