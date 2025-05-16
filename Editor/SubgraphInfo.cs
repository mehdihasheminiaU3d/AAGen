using System.Collections.Generic;
using System.Linq;
using AAGen.AssetDependencies;

namespace AAGen
{
    public class SubgraphInfo
    {
        public bool IsShared; 
        public HashSet<AssetNode> Nodes = new HashSet<AssetNode>();

        public int Hash;
        public SubgraphCategoryID CategoryID;
        public HashSet<AssetNode> Sources = new HashSet<AssetNode>();

        public static List<SubgraphInfo> SelectSubgraphsByCategory(List<SubgraphInfo> allSubgraphs, SubgraphCategoryID categoryID)
        {
            return allSubgraphs.Where(subgraph => subgraph.CategoryID == categoryID).ToList();
        }
    }
}