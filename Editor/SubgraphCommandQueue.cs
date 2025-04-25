using System.Collections.Generic;
using System.Linq;
using AAGen.AssetDependencies;
using UnityEngine;

namespace AAGen
{
    internal class SubgraphCommandQueue : CommandQueue
    {
        readonly DataContainer m_DataContainer;
        
        public SubgraphCommandQueue(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;
            Title = nameof(SubgraphCommandQueue);
        }

        public override void PreExecute()
        {
            m_DataContainer._allSubgraphs = new Category();
            m_DataContainer._subgraphSources = new Dictionary<int, HashSet<AssetNode>>();
            
            var nodes = m_DataContainer.m_DependencyGraph.GetAllNodes();
            foreach (var node in nodes)
            {
                AddCommand(new ActionCommand(() => TryAddNodeToSubgraph(node), node.AssetPath));
            }
            
            EnqueueCommands();
        }

        void TryAddNodeToSubgraph(AssetNode node)
        {
            var sources = FindSourcesForNode(node, m_DataContainer.m_TransposedGraph, m_DataContainer.IgnoredAssets);
            if (sources == null)
                return;

            var subgraph = new SubgraphInfo
            {
                IsShared = sources.Count > 1
            };
                
            int hash = CalculateHashForSources(sources);

            if (!m_DataContainer._subgraphSources.TryAdd(hash, sources))
            {
                if (!m_DataContainer._subgraphSources[hash].SetEquals(sources))
                    Debug.LogError($"Hash collision = inconsistent sources for subgraph {hash}");
            }
                
            //uniqueness of Hash is key! if we want to validate uniqueness of sources, then we need to save them
            //Sources cannot be saved in subgraph. Because sources can be redundant for each record leading to a very large file
            m_DataContainer._allSubgraphs.TryAdd(hash, subgraph);
            var nodeAdditionSuccess = m_DataContainer._allSubgraphs[hash].Nodes.Add(node);
            
            if (!nodeAdditionSuccess)
                Debug.LogError($"Unknown Error = node = {node} had added to subgraph ={hash} before");
        }
        
        static int CalculateHashForSources(HashSet<AssetNode> sources)
        {
            int hash = 17; 

            foreach (var source in sources)
            {
                hash = hash * 31 + source.Guid.GetHashCode();
            }

            return hash;
        }

        static HashSet<AssetNode> FindSourcesForNode(AssetNode node, DependencyGraph transposedGraph, HashSet<AssetNode> ignoredAssets)
        {
            if (ignoredAssets.Contains(node)) //ignore sources in specified folders
                return null;

            var allPaths = transposedGraph.DepthFirstSearchForAllPaths(node, IsSource);
            var sources = allPaths.Select(path => path[^1]).ToHashSet();
                
            if(sources.IsSubsetOf(ignoredAssets)) //ignore assets exclusive to ignored sources 
                return null;
                
            sources.ExceptWith(ignoredAssets);
            return sources;
            
            bool IsSource(AssetNode endNode)
            {
                return transposedGraph.GetNeighbors(endNode).Count == 0;
            }
        }
    }
}