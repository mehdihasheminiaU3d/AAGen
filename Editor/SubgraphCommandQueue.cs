using System;
using System.Collections.Generic;
using System.Linq;
using AAGen.AssetDependencies;
using UnityEngine;

namespace AAGen
{
    internal class SubgraphCommandQueue : CommandQueue
    {
        readonly DataContainer m_DataContainer;

        int m_NodesProcessed = 0;
        
        public SubgraphCommandQueue(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;
            Title = nameof(SubgraphCommandQueue);
        }

        public override void PreExecute()
        {
            m_DataContainer.Subgraphs = new Dictionary<int, SubgraphInfo>();
            
            var nodes = m_DataContainer.DependencyGraph.GetAllNodes();
            foreach (var node in nodes)
            {
                AddCommand(new ActionCommand(() => TryAddNodeToSubgraph(node), node.AssetPath));
            }
            
            EnqueueCommands();
        }

        void TryAddNodeToSubgraph(AssetNode node)
        {
            var sources = FindSourcesForNode(node, m_DataContainer.TransposedGraph, m_DataContainer.IgnoredAssets);
            if (sources == null)
                return;
            
            m_NodesProcessed++;
            
            int hash = CalculateHashForSources(sources);

            if (m_DataContainer.Subgraphs.TryGetValue(hash, out var existingSubgraph))
            {
                //Double check the uniqueness of hash for the set of sources
                if (!existingSubgraph.Sources.SetEquals(sources))
                    throw new Exception($"Hash collision = inconsistent sources for subgraph {hash}");
            }
            else
            {
                var newSubgraph = new SubgraphInfo
                {
                    Sources = sources,
                    IsShared = sources.Count > 1, //ToDo: Can be a property
                    CategoryID = m_DataContainer.Settings.DefaultCategoryID
                };
                
                m_DataContainer.Subgraphs.Add(hash, newSubgraph);
            }
            
            var result = m_DataContainer.Subgraphs[hash].Nodes.Add(node);
                
            //Make sure the node isn't added before. If so, it can indicate a problem in our logic.
            if (!result)
                throw new Exception($"Unknown Error = node = {node} had added to subgraph ={hash} before");
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
        
        public override void PostExecute()
        {
            AppendToSummaryReport();
        }

        void AppendToSummaryReport()
        {
            if (!m_DataContainer.Settings.GenerateSummaryReport)
                return;

            var summary = $"\n=== Subgraphs ===\n";
            summary += $"{nameof(m_NodesProcessed).ToReadableFormat()} = {m_NodesProcessed} \n";
            summary += $"Subgraph Count = {m_DataContainer.Subgraphs.Count} \n";
            summary += $"Topologies:\n";
            summary += CategorizeSubgraphs();
            
            m_DataContainer.SummaryReport.AppendLine(summary);
        }

        string CategorizeSubgraphs()
        {
            var hierarchies = new Category();
            var singleAssets = new Category();
            var sharedAssets = new Category();
            var m_sharedSingleSinks = new Category();
            var k_sharedSingles = new Category();
            var singleSources = new Category();
            var exclusiveToSingleSource = new Category();

            foreach (var pair in m_DataContainer.Subgraphs)
            {
                var hash = pair.Key;
                var subgraph = pair.Value;
                var nodes = subgraph.Nodes;
                var sources = subgraph.Sources;
                bool isShared = sources.Count > 1;

                if (nodes.Count == 0)
                    Debug.LogError($"{hash} has no nodes");

                if (sources.Count == 0)
                    Debug.LogError($"{hash} has no sources");

                if (nodes.Count == 1)
                {
                    var singleNode = nodes.ToList()[0];
                    var outgoingEdges = CountOutgoingEdges(singleNode);
                    var incomingEdges = CountIncomingEdges(singleNode);
                    if (incomingEdges == 0 && outgoingEdges == 0)
                    {
                        singleAssets.Add(hash, subgraph);
                        continue;
                    }

                    if (isShared && outgoingEdges == 0) // consists only of a single sink node
                    {
                        m_sharedSingleSinks.Add(hash, subgraph);
                        continue;
                    }

                    if (incomingEdges == 0) // consists only of a single source node
                    {
                        singleSources.Add(hash, subgraph);
                        continue;
                    }
                }
                else
                {
                    if (sources.IsSubsetOf(nodes))
                    {
                        //Hierarchies always have one source because that source needs to be its own source as well
                        if (sources.Count > 1)
                            Debug.LogError($"{hash} Unknown hierarchy {sources.Count}");

                        hierarchies.Add(hash, subgraph);
                        continue;
                    }
                }

                if (isShared) //Has more than one source
                {
                    if (nodes.Count == 1)
                        k_sharedSingles.Add(hash, subgraph);
                    else
                        sharedAssets.Add(hash, subgraph);
                    continue;
                }
                else
                {
                    //These are due to cycles and the fact hat cycle nodes doesn't follow the rule of incoming/outgoing node count 
                    exclusiveToSingleSource.Add(hash, subgraph);
                }
            }

            var total = hierarchies.Count + singleAssets.Count + sharedAssets.Count + m_sharedSingleSinks.Count +
                        k_sharedSingles.Count + singleSources.Count + exclusiveToSingleSource.Count;

            const string indent = "    ";
            var summary = $"{indent}{nameof(hierarchies).ToReadableFormat()} : {hierarchies.Count}\n";
            summary+=  $"{indent}{nameof(singleAssets).ToReadableFormat()} : {singleAssets.Count}\n";
            summary+=  $"{indent}{nameof(sharedAssets).ToReadableFormat()} : {sharedAssets.Count}\n";
            summary+=  $"{indent}{nameof(m_sharedSingleSinks).ToReadableFormat()} : {m_sharedSingleSinks.Count}\n";
            summary+=  $"{indent}{nameof(k_sharedSingles).ToReadableFormat()} : {k_sharedSingles.Count}\n";
            summary+=  $"{indent}{nameof(singleSources).ToReadableFormat()} : {singleSources.Count}\n";
            summary+=  $"{indent}{nameof(exclusiveToSingleSource).ToReadableFormat()} : {exclusiveToSingleSource.Count}\n";
            summary+=  $"{indent}----------\n";
            summary+=  $"{indent}{nameof(total).ToReadableFormat()} : {total}\n";
            
            return summary;

            int CountOutgoingEdges(AssetNode node)
            {
                return m_DataContainer.DependencyGraph.GetNeighbors(node).Count;
            }

            int CountIncomingEdges(AssetNode node)
            {
                return m_DataContainer.DependencyGraph.GetNeighbors(node).Count;
            }
        }
    }
}