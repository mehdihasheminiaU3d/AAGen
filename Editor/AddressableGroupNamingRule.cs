using UnityEngine;

namespace AAGen
{
    public abstract class AddressableGroupNamingRule : ScriptableObject
    {
        public SubgraphCategoryID m_CategoryID;
        
        public abstract string GetGroupName(int hash, SubgraphInfo subgraph, DataContainer dataContainer);
    }
    
    public class DefaultNamingRule : AddressableGroupNamingRule
    {
        public override string GetGroupName(int hash, SubgraphInfo subgraph, DataContainer dataContainer)
        {
            return hash.ToString();
        }
    }
}