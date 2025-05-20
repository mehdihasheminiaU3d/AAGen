using UnityEngine;

namespace AAGen
{
    public abstract class SubgraphCategoryID : ScriptableObject
    {
        //ToDo: Should ID and Selection criteria be separate classes?
        
        public abstract bool DoesSubgraphMatchCategory(SubgraphInfo subgraph, DataContainer dataContainer);
    }
    
    public class UncategorizedSubgraphID : SubgraphCategoryID
    {
        public override bool DoesSubgraphMatchCategory(SubgraphInfo subgraph, DataContainer dataContainer) => true;
    }
}