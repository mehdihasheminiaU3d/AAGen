using System.Collections.Generic;
using UnityEngine;

namespace AAGen.Editor.DependencyGraph
{
    /// <summary>
    /// Contains data and logic to merge subgraphs, reducing their overall number and ultimately decreasing the number of addressable groups
    /// </summary>
    internal abstract class MergeRule : ScriptableObject
    {
        [SerializeField]
        private CategoryId _OriginCategory;
        [SerializeField]
        private CategoryId _DestinationCategory;

        public CategoryId OriginCategory => _OriginCategory;
        public CategoryId DestinationCategory => _DestinationCategory;

        public bool FromCondition(KeyValuePair<int, SubgraphInfo> entry)
        {
            var hash = entry.Key;
            var subgraph = entry.Value;
            return SelectSubgraphsOfOriginCategory(subgraph);
        }

        public bool ToCondition(KeyValuePair<int, SubgraphInfo> entry)
        {
            var hash = entry.Key;
            var subgraph = entry.Value;
            return SelectSubgraphsOfDestinationCategory(subgraph);
        }

        public virtual bool SelectSubgraphsOfOriginCategory(SubgraphInfo subgraphInfo) => false;
        public virtual bool SelectSubgraphsOfDestinationCategory(SubgraphInfo subgraphInfo) => false;
    }
}
