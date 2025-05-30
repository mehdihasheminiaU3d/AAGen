using System.Collections.Generic;
using AAGen.AssetDependencies;

namespace AAGen
{
    public class SubgraphInfo
    {
        //Obsolete
        public bool IsShared;
        
        public HashSet<AssetNode> Nodes = new HashSet<AssetNode>(); //ToDo: pros & cons of using hashsets here?
        public HashSet<AssetNode> Sources = new HashSet<AssetNode>();
        public string Name;
        public int HashOfSources;
        public string AddressableTemplateName;
    }
}