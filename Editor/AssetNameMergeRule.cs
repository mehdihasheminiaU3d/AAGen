using System;
using System.Collections.Generic;
using UnityEngine;

namespace AAGen
{
    [CreateAssetMenu(menuName = "Dependency Graph/Automated Asset Grouping/" + nameof(AssetNameMergeRule))]
    internal class AssetNameMergeRule : MergeRule
    {
        [SerializeField]
        private List<string> _OriginKeywords;
        [SerializeField]
        private List<string> _DestinationKeywords;

        public override bool SelectSubgraphsOfOriginCategory(SubgraphInfo subgraphInfo)
        {
            if (_OriginKeywords == null || _OriginKeywords.Count == 0) //no keyword is set
                return true; //return all

            foreach (var keyword in _OriginKeywords)
            {
                foreach (var node in subgraphInfo.Nodes)
                {
                    if (node.AssetPath.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }
        
        public override bool SelectSubgraphsOfDestinationCategory(SubgraphInfo subgraphInfo)
        {
            if (_DestinationKeywords == null || _DestinationKeywords.Count == 0) //no keyword is set
                return true; //return all
            
            foreach (var keyword in _DestinationKeywords)
            {
                foreach (var node in subgraphInfo.Nodes)
                {
                    if (node.AssetPath.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            
            return false;
        }
    }
}
