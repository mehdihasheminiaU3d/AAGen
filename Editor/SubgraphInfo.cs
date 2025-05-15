using System.Collections.Generic;
using System.Linq;
using AAGen.AssetDependencies;

namespace AAGen
{
    public class SubgraphInfo
    {
        public SubgraphCategoryID CategoryID;
        public bool IsShared; 
        public HashSet<AssetNode> Nodes = new HashSet<AssetNode>();

        public static List<SubgraphInfo> SelectSubgraphsByCategory(List<SubgraphInfo> allSubgraphs, SubgraphCategoryID categoryID)
        {
            return allSubgraphs.Where(subgraph => subgraph.CategoryID == categoryID).ToList();
        }
    }
}