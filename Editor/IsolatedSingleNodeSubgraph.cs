using System.Linq;
using AAGen.AssetDependencies;
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
                var outgoingEdges = CountOutgoingEdges(node);
                var incomingEdges = CountIncomingEdges(node);
                if (incomingEdges == 0 && outgoingEdges == 0)
                {
                    return true;
                }
            }
            
            return false;
            
            int CountOutgoingEdges(AssetNode node)
            {
                return dataContainer.DependencyGraph.GetNeighbors(node).Count;
            }
            
            int CountIncomingEdges(AssetNode node)
            {
                return dataContainer.TransposedGraph.GetNeighbors(node).Count;
            }
        }
    }
}