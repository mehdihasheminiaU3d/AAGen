using System.Linq;
using UnityEngine;

namespace AAGen
{
    [CreateAssetMenu(menuName = "AAGen/Settings/Subgraph Categories/" + nameof(IsolatedSingleNodeSubgraph))]
    public class IsolatedSingleNodeSubgraph : SubgraphCategoryID
    {
        public override bool DoesSubgraphMatchCategory(SubgraphInfo subgraph, DataContainer dataContainer)
        {
            if (subgraph.Nodes.Count == 1) 
            {
                var node = subgraph.Nodes.ToList()[0];
                var outgoingEdges = dataContainer.DependencyGraph.CountOutgoingEdges(node);
                var incomingEdges = dataContainer.DependencyGraph.CountIncomingEdges(node);
                if (incomingEdges == 0 && outgoingEdges == 0)
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}