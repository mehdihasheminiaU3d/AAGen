using System.Collections.Generic;
using System.Linq;
using AAGen.AssetDependencies;
using UnityEngine;

namespace AAGen
{
    [CreateAssetMenu(menuName = "AAGen/Settings/Refinement Rules/" + nameof(MergeCategoryRule))]
    public class MergeCategoryRule : RefinementRule
    {
        public SubgraphCategoryID m_CategoryID;
        
        public override void Execute(DataContainer dataContainer)
        {
            var categories = dataContainer.GetSubgraphsGroupedByCategory();
            MergeSubgraphs(categories[m_CategoryID], dataContainer);
        }
        
        void MergeSubgraphs(List<SubgraphInfo> subgraphs, DataContainer dataContainer)
        {
            //Extract subgraph data
            var allNodes = new HashSet<AssetNode>();
            var allSources = new HashSet<AssetNode>();
            var hashes = new List<int>();
            foreach (var subgraph in subgraphs)
            {
                allNodes.UnionWith(subgraph.Nodes);
                allSources.UnionWith(subgraph.Sources);
                hashes.Add(SubgraphCommandQueue.CalculateHashForSources(subgraph.Sources)); //<----need for recalculating the hash!
            }
            
            //Remove previous subgraphs //<-depends on hash algorithm
            foreach (int key in hashes)
            {
                dataContainer.Subgraphs.Remove(key); 
            }
            
            var newSubgraph = new SubgraphInfo
            {
                Nodes = allNodes,
                Sources = allSources,
                IsShared = allSources.Count > 1, //ToDo: Can be a property 
                CategoryID = dataContainer.Settings.DefaultCategoryID 
            };
            
            dataContainer.Subgraphs.Add(SubgraphCommandQueue.CalculateHashForSources(allSources), newSubgraph);//<----hash could be a filed need for recalculating the hash!
        }
        
        //<----- ToDo: Needs categorization 
        //<----------- it easily breaks the organization based on the dependency chain
    }
}