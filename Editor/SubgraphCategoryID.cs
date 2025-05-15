using UnityEngine;

namespace AAGen
{
    public abstract class SubgraphCategoryID : ScriptableObject
    {
        public abstract bool MatchesCategoryRule(SubgraphInfo subgraph);
    }
    
    public class UncategorizedSubgraphID : SubgraphCategoryID
    {
        public override bool MatchesCategoryRule(SubgraphInfo subgraph) => true;
    }
}