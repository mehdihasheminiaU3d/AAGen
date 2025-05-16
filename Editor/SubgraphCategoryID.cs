using UnityEngine;

namespace AAGen
{
    public abstract class SubgraphCategoryID : ScriptableObject
    {
        public abstract bool DoesSubgraphMatchCategory(SubgraphInfo subgraph);
    }
    
    public class UncategorizedSubgraphID : SubgraphCategoryID
    {
        public override bool DoesSubgraphMatchCategory(SubgraphInfo subgraph) => true;
    }
}