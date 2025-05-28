using System.Collections.Generic;
using AAGen.AssetDependencies;

namespace AAGen
{
    public class SubgraphInfo
    {
        //ToDo: pros & cons of using hashsets here?
        
        public bool IsShared; 
        public HashSet<AssetNode> Nodes = new HashSet<AssetNode>(); 
        public HashSet<AssetNode> Sources = new HashSet<AssetNode>();
        public string Name;
        public int HashOfSources;
    }
}