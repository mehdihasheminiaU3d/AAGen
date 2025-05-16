using System.Collections.Generic;
using System.Linq;
using AAGen.AssetDependencies;

namespace AAGen
{
    public class SubgraphInfo
    {
        public bool IsShared; 
        public HashSet<AssetNode> Nodes = new HashSet<AssetNode>();
        
        public SubgraphCategoryID CategoryID;
        public HashSet<AssetNode> Sources = new HashSet<AssetNode>();
    }
}