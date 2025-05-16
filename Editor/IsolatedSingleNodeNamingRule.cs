using System.Linq;
using UnityEngine;

namespace AAGen
{
    [CreateAssetMenu(menuName = "AAGen/Settings/Subgraph Categories/" + nameof(IsolatedSingleNodeNamingRule))]
    public class IsolatedSingleNodeNamingRule : AddressableGroupNamingRule
    {
        public override string CalculateGroupName(int hash, SubgraphInfo subgraph)
        {
            var node = subgraph.Nodes.ToList()[0];
            return $"Isolated Node {node.FileName}";
        }
    }
}