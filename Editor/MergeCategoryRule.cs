using System.Collections.Generic;
using AAGen.AssetDependencies;
using AAGen.Shared;
using UnityEngine;

namespace AAGen
{
    [CreateAssetMenu(menuName = Constants.ContextMenus.OutputRulesMenu + nameof(MergeCategoryRule))]
    public class MergeCategoryRule : RefinementRule
    {
        public override void Execute(List<SubgraphInfo> subgraphs)
        {
            MergeSubgraphs(subgraphs);
        }

        bool CanMerge(SubgraphInfo subgraphA, SubgraphInfo subgraphB)
        {
            return subgraphA.Sources.IsSubsetOf(subgraphB.Sources) || 
                   subgraphB.Sources.IsSubsetOf(subgraphA.Sources);
        }

        void RemoveSubgraphs(DataContainer dataContainer, SubgraphInfo subgraph)
        {
            
        }

        void AddSubgraph(DataContainer dataContainer, SubgraphInfo subgraph)
        {
            
        }

        void AddNodeToSubgraph()
        {
            
        }
        
        void MergeSubgraphs(List<SubgraphInfo> subgraphs)
        {
            //Extract subgraph data
            // var allNodes = new HashSet<AssetNode>();
            // var allSources = new HashSet<AssetNode>();
            // var hashes = new List<int>();
            // foreach (var subgraph in subgraphs)
            // {
            //     allNodes.UnionWith(subgraph.Nodes);
            //     allSources.UnionWith(subgraph.Sources);
            //     hashes.Add(SubgraphCommandQueue.CalculateHashForSources(subgraph.Sources)); //<----need for recalculating the hash!
            // }
            //
            // //Remove previous subgraphs //<-depends on hash algorithm
            // foreach (int key in hashes)
            // {
            //     dataContainer.Subgraphs.Remove(key); 
            // }
            //
            // var newSubgraph = new SubgraphInfo
            // {
            //     Nodes = allNodes,
            //     Sources = allSources,
            //     IsShared = allSources.Count > 1, //ToDo: Can be a property 
            //     CategoryID = dataContainer.Settings.DefaultCategoryID 
            // };
            //
            // dataContainer.Subgraphs.Add(SubgraphCommandQueue.CalculateHashForSources(allSources), newSubgraph);//<----hash could be a filed need for recalculating the hash!
        }
    }
}