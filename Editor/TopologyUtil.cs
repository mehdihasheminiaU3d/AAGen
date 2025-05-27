using System.Linq;
using AAGen.AssetDependencies;

namespace AAGen
{
    public static class SubgraphTopologyUtil
    {
        public static bool IsShared(SubgraphInfo subgraph)
        {
            return subgraph.Sources.Count > 1;
        }
        
        /// <summary>
        /// Return true if the input subgraph consists of a single node that has no connection to other nodes
        /// </summary>
        public static bool IsSingleIsolatedNode(SubgraphInfo subgraph, DependencyGraph dependencyGraph)
        {
            if (subgraph.Nodes.Count == 1)
            {
                var singleNode = subgraph.Nodes.ToList()[0];
                if (dependencyGraph.IsSourceNode(singleNode) && dependencyGraph.IsSinkNode(singleNode))
                    return true;
            }

            return false;
        }
        
        /// <summary>
        /// Return true if the input subgraph consists of a single node that is a source node of other subgraphs
        /// </summary>
        public static bool IsSingleSourceNode(SubgraphInfo subgraph, DependencyGraph dependencyGraph)
        {
            if (subgraph.Nodes.Count == 1)
            {
                var singleNode = subgraph.Nodes.ToList()[0];
                if (dependencyGraph.IsSourceNode(singleNode) && !dependencyGraph.IsSinkNode(singleNode))
                    return true;
            }

            return false;
        }
        
        /// <summary>
        /// Return true if the input subgraph consists of a single node that is a sink node for other subgraphs
        /// </summary>
        public static bool IsSingleSinkNode(SubgraphInfo subgraph, DependencyGraph dependencyGraph)
        {
            if (subgraph.Nodes.Count == 1)
            {
                var singleNode = subgraph.Nodes.ToList()[0];
                if (!dependencyGraph.IsSourceNode(singleNode) && dependencyGraph.IsSinkNode(singleNode))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Return true if the input subgraph consists of multiple connected nodes including all the source nodes
        /// </summary>
        public static bool IsHierarchy(SubgraphInfo subgraph, DependencyGraph dependencyGraph)
        {
            return subgraph.Nodes.Count > 1 && subgraph.Sources.IsSubsetOf(subgraph.Nodes);
        }
    }
}