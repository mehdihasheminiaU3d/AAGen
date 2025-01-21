using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AAGen.Runtime;
using Unity.EditorCoroutines.Editor;
using UnityEngine;

namespace AAGen
{
    //Nodes of a graph with identical set of sources
    internal class SubgraphInfo
    {
        public bool IsShared; 
        public HashSet<AssetNode> Nodes = new HashSet<AssetNode>();
    }
    
    internal class SubgraphProcessor : DependencyGraphProcessor
    {
        public SubgraphProcessor(DependencyGraph dependencyGraph, EditorUiGroup uiGroup) 
            : base(dependencyGraph, uiGroup) {}
        
        private static string _filePath => Path.Combine(Constants.FolderPath, "Subgraphs.txt");
        
        private Category _allSubgraphs;
        private DependencyGraph _transposedGraph;
        private EditorJobGroup _sequence;
        private Dictionary<int,HashSet<AssetNode>> _subgraphSources;
        private HashSet<AssetNode> _ignoredAssets;
        
        private string _result;
        
        public IEnumerator Execute()
        {
            _sequence = new EditorJobGroup(nameof(SubgraphProcessor));
            _sequence.AddJob(new ActionJob(Init, nameof(Init)));
            _sequence.AddJob(new CoroutineJob(LoadIgnoredAssetsList, nameof(LoadIgnoredAssetsList)));
            _sequence.AddJob(new CoroutineJob(CreateSubgraphs, nameof(CreateSubgraphs)));
            _sequence.AddJob(new CoroutineJob(SaveSubgraphsToFile, nameof(SaveSubgraphsToFile)));
            _sequence.AddJob(new ActionJob(DisplayResultsOnUi, nameof(DisplayResultsOnUi)));
            _sequence.AddJob(new ActionJob(FreeMemory, nameof(FreeMemory)));
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(_sequence.Run());
        }

        protected override void Init()
        {
            base.Init();
            _transposedGraph = new DependencyGraph(_dependencyGraph.GetTransposedGraph());
        }
        
        private IEnumerator CreateSubgraphs()
        {
            _allSubgraphs = new Category();
            _subgraphSources = new Dictionary<int, HashSet<AssetNode>>();
            
            var nodes = _dependencyGraph.GetAllNodes();
            
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];

                var sources = FindSourcesForNode(node, _transposedGraph, _ignoredAssets);
                if (sources == null)
                    continue;

                var subgraph = new SubgraphInfo
                {
                    IsShared = sources.Count > 1
                };
                int hash = CalculateHashForSources(sources);

                if (!_subgraphSources.TryAdd(hash, sources))
                {
                    if (!_subgraphSources[hash].SetEquals(sources))
                        OnError($"Hash collision = inconsistent sources for subgraph {hash}");
                }
                
                //uniqueness of Hash is key! if we want to validate uniqueness of sources, then we need to save them
                //Sources cannot be saved in subgraph. Because sources can be redundant for each record leading to a very large file
                _allSubgraphs.TryAdd(hash, subgraph);
                var nodeAdditionSuccess = _allSubgraphs[hash].Nodes.Add(node);
                if (!nodeAdditionSuccess)
                    OnError($"Unknown Error = node = {node} had added to subgraph ={hash} before");

                if (ShouldUpdateUi) 
                {
                    _sequence.ReportProgress((float)i / nodes.Count, node.FileName);
                    yield return null;
                }
            }
            
            yield break;
        }
      
        private void DisplayResultsOnUi()
        {
            if (_uiGroup == null)
            {
                Debug.Log($"Subgrahs generation completed!");
                return;
            }
            
            _result += $"Total subgraphs = {_allSubgraphs.Count} \n";

            //File extension statistics
            var extCount = new Dictionary<string, int>();
            foreach (var subgraph in _allSubgraphs.Values)
            {
                foreach (var node in subgraph.Nodes)
                {
                    var ext = Path.GetExtension(node.AssetPath).ToLower();
                    if (extCount.ContainsKey(ext))
                    {
                        extCount[ext] += 1;
                    }
                    else
                    {
                        extCount.Add(ext, 1);
                    }
                }
            }

            _result += "\n File Types :";
            foreach (var pair in extCount)
            {
                _result += $"\n {pair.Key} = {pair.Value}";
            }
            
            _uiGroup.UIVisibility |= EditorUiGroup.UIVisibilityFlag.ShowOutput;
            _uiGroup.OutputText = _result;
        }
        private void FreeMemory()
        {
            _transposedGraph = null;
            _ignoredAssets = null;
            _subgraphSources = null;
        }
        
        private IEnumerator SaveSubgraphsToFile()
        {
            yield return DependencyGraphUtil.SaveToFileAsync(_allSubgraphs, _filePath, (success) =>
            {
                if (!success)
                    OnError($">>> Subgraphs failed to Save!");
            });
        }
        
        private IEnumerator LoadIgnoredAssetsList()
        {
            string ignoredFilePath = Path.Combine(Constants.FolderPath, "IgnoredAssets.txt");
        
            yield return DependencyGraphUtil.LoadFromFileAsync<HashSet<AssetNode>>(ignoredFilePath,
                (data) => { _ignoredAssets = data; });
        }
        
        public static int CalculateHashForSources(HashSet<AssetNode> sources)
        {
            int hash = 17; 

            foreach (var source in sources)
            {
                hash = hash * 31 + source.Guid.GetHashCode();
            }

            return hash;
        }

        public static HashSet<AssetNode> FindSourcesForNode(AssetNode node, DependencyGraph transposedGraph, HashSet<AssetNode> ignoredAssets)
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

        private static HashSet<AssetNode> FindSourcesForSubGraph(SubgraphInfo subgraph, DependencyGraph transposedGraph, HashSet<AssetNode> ignoredAssets)
        {
            //All nodes in a subgraph has the same set of sources
            if (subgraph.Nodes.Count == 0)
                return null;

            return FindSourcesForNode(subgraph.Nodes.ToList()[0], transposedGraph, ignoredAssets);
        }

        private void OnError(string message)
        {
            Debug.LogError(message);
            _sequence.Cancel();
        }
    }
}
