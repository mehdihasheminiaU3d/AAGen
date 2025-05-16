using UnityEngine;

namespace AAGen
{
    public abstract class AddressableGroupNamingRule : ScriptableObject
    {
        public SubgraphCategoryID m_CategoryID;
        
        public abstract string CalculateGroupName(int hash, SubgraphInfo subgraph);
    }
    
    public class DefaultNamingRule : AddressableGroupNamingRule
    {
        public override string CalculateGroupName(int hash, SubgraphInfo subgraph)
        {
            return hash.ToString();
        }
    }
}