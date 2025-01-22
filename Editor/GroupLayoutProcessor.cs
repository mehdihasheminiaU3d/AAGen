using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AAGen.Runtime;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace AAGen
{
    internal class Category : Dictionary<int, SubgraphInfo>
    {
        public bool CanMoveFrom;
        public bool CanMoveTo;
        public bool MergeAllBeforeGrouping;
        
        public int CountNodes() => Values.Sum(subgraph => subgraph.Nodes.Count);
    }
    
    [Serializable]
    internal class GroupLayoutInfo
    {
        public string TemplateName;
        public List<AssetNode> Nodes = new List<AssetNode>();
    }

    internal enum CategoryId
    {
        Hierarchies,
        SingleAssets,
        SharedAssets,
        SharedSingles,
        SharedSingleSinks,
        SingleSources,
        ExclusiveToSingleSource,
    }
    
    internal class GroupLayoutProcessor : DependencyGraphProcessor
    {
        public GroupLayoutProcessor(DependencyGraph dependencyGraph, EditorUiGroup uiGroup, AutomatedAssetGrouper parentUi)
            : base(dependencyGraph, uiGroup)
        {
            _parentUi = parentUi;
        }
        
        private static string _inputFilePath => Path.Combine(Constants.FolderPath, "Subgraphs.txt");
        private static string _groupLayoutFilePath => Path.Combine(Constants.FolderPath, "GroupLayout.txt");
        
        private EditorJobGroup _sequence;
        private AutomatedAssetGrouper _parentUi;
        private string _result;
        private DependencyGraph _transposedGraph;
        private HashSet<AssetNode> _ignoredAssets;
        private Category _allSubgraphs;
        private Dictionary<string, GroupLayoutInfo> _groupLayout;
        private Dictionary<int,HashSet<AssetNode>> _SubgraphSources;
        private double startTime;
        
        private Dictionary<CategoryId, Category> _Categories;
        private Category _hierarchies;
        private Category _singleAssets;
        private Category _sharedAssets;
        private Category _sharedSingleSinks;
        private Category _sharedSingles;
        private Category _singleSources;
        private Category _ExclusiveToSingleSource;
        
        public IEnumerator Execute()
        {
            startTime = EditorApplication.timeSinceStartup;
            _result = null;

            _sequence = new EditorJobGroup(nameof(GroupLayoutProcessor));
            _sequence.AddJob(new ActionJob(Init, nameof(Init)));
            _sequence.AddJob(new CoroutineJob(LoadIgnoredAssetsList, nameof(LoadIgnoredAssetsList)));
            _sequence.AddJob(new CoroutineJob(LoadAllSubgraphsFromFile, nameof(LoadAllSubgraphsFromFile)));
            _sequence.AddJob(new CoroutineJob(FindSubgraphSources, nameof(FindSubgraphSources)));
            _sequence.AddJob(new CoroutineJob(CategorizeSubgraphs, nameof(CategorizeSubgraphs)));
            _sequence.AddJob(new ActionJob(PrintCategories, nameof(PrintCategories)));

            foreach (var mergeRule in _parentUi.Settings._MergeRules)
            {
                _sequence.AddJob(new CoroutineJob(() => MoveSubgraphsByRule(mergeRule), mergeRule.GetType().Name));
                _sequence.AddJob(new ActionJob(PrintCategories, nameof(PrintCategories)));
            }

            foreach (var groupLayoutRule in _parentUi.Settings._GroupLayoutRules)
            {
                _sequence.AddJob(new CoroutineJob(() => AddToGroupLayoutByRule(groupLayoutRule), nameof(AddToGroupLayoutByRule)));
                _sequence.AddJob(new CoroutineJob(SaveGroupLayout, nameof(SaveGroupLayout)));
            }
            
            _sequence.AddJob(new CoroutineJob(SaveGroupLayout, nameof(SaveGroupLayout)));
            _sequence.AddJob(new ActionJob(Verify, nameof(Verify)));
            _sequence.AddJob(new ActionJob(DisplayResultsOnUi, nameof(DisplayResultsOnUi)));
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(_sequence.Run());
        }
        
        protected override void Init()
        {
            _transposedGraph = new DependencyGraph(_dependencyGraph.GetTransposedGraph());
            DefineCategories();
            _groupLayout = new Dictionary<string, GroupLayoutInfo>();
        }

        private void DefineCategories()
        {
            _Categories = new Dictionary<CategoryId, Category>()
            {
                { CategoryId.Hierarchies , new Category()}, 
                { CategoryId.SingleAssets , new Category()},
                { CategoryId.SharedAssets , new Category()}, 
                { CategoryId.SharedSingles , new Category()}, 
                { CategoryId.SharedSingleSinks , new Category()}, 
                { CategoryId.SingleSources , new Category()},
                { CategoryId.ExclusiveToSingleSource , new Category()},
            };
            
            _hierarchies = _Categories[CategoryId.Hierarchies];
            _hierarchies.CanMoveFrom = false;
            _hierarchies.CanMoveTo = false;
            _hierarchies.MergeAllBeforeGrouping = false;
            
            _singleAssets = _Categories[CategoryId.SingleAssets];
            _singleAssets.CanMoveFrom = true;
            _singleAssets.CanMoveTo = false;
            _singleAssets.MergeAllBeforeGrouping = true;
            
            _sharedAssets = _Categories[CategoryId.SharedAssets];
            _sharedAssets.CanMoveFrom = false;
            _sharedAssets.CanMoveTo = true;
            _sharedAssets.MergeAllBeforeGrouping = false;
            
            _sharedSingleSinks = _Categories[CategoryId.SharedSingleSinks];
            _sharedSingleSinks.CanMoveFrom = false;
            _sharedSingleSinks.CanMoveTo = true;
            _sharedSingleSinks.MergeAllBeforeGrouping = true;
            
            _sharedSingles = _Categories[CategoryId.SharedSingles];
            _sharedSingles.CanMoveFrom = false;
            _sharedSingles.CanMoveTo = true;
            _sharedSingles.MergeAllBeforeGrouping = false;
            
            _singleSources = _Categories[CategoryId.SingleSources];
            _singleSources.CanMoveFrom = true;
            _singleSources.CanMoveTo = false;
            _singleSources.MergeAllBeforeGrouping = true;
            
            _ExclusiveToSingleSource = _Categories[CategoryId.ExclusiveToSingleSource];
            _ExclusiveToSingleSource.CanMoveFrom = false;
            _ExclusiveToSingleSource.CanMoveTo = false;
            _ExclusiveToSingleSource.MergeAllBeforeGrouping = false;
        }
        
        private IEnumerator LoadAllSubgraphsFromFile()
        {
            yield return DependencyGraphUtil.LoadFromFileAsync<Category>(_inputFilePath,
                (data) => { _allSubgraphs = data; });
        }
        
        private IEnumerator LoadIgnoredAssetsList()
        {
            string ignoredFilePath = Path.Combine(Constants.FolderPath, "IgnoredAssets.txt");
        
            yield return DependencyGraphUtil.LoadFromFileAsync<HashSet<AssetNode>>(ignoredFilePath,
                (data) => { _ignoredAssets = data; });
        }
        
        private IEnumerator FindSubgraphSources()
        {
            _SubgraphSources = new Dictionary<int,HashSet<AssetNode>>();
            
            var nodes = _dependencyGraph.GetAllNodes();
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];

                var sources = SubgraphProcessor.FindSourcesForNode(node, _transposedGraph, _ignoredAssets);
                if (sources == null)
                    continue;
                
                int subgraphNameHash = SubgraphProcessor.CalculateHashForSources(sources);

                _SubgraphSources.TryAdd(subgraphNameHash, sources);
                
                if (ShouldUpdateUi)
                {
                    _sequence.ReportProgress((float)i/nodes.Count, $"Recalculate sources for {node.FileName}");
                    yield return null;
                }
            }
        }
        
        private static string GetSubgraphName(SubgraphInfo subgraph, HashSet<AssetNode> sources)
        {
            string name = null;
            if (subgraph.IsShared) 
            {
                name = $"Shared_";
                if (subgraph.Nodes.Count == 1)
                {
                    var node = subgraph.Nodes.ToList()[0];
                    name += node.FileName;
                }
                else
                {
                    name += SubgraphProcessor.CalculateHashForSources(sources).ToString();
                }
            }
            else
            {
                if(sources is { Count: > 0 })
                {
                    var sourceNode = sources.ToList()[0];
                    var n = sourceNode.FileName;
                    name = $"{n}_Assets";
                }
            }
            
            if (string.IsNullOrEmpty(name)) // ToDo: Is this condition ever met?
            {
                name = $"NullName_{Guid.NewGuid().ToString()}"; 
                Debug.LogError($"empty subgraph name");
            }
                
            return name;
        }
        
        private IEnumerator SaveGroupLayout()
        {
            yield return DependencyGraphUtil.SaveToFileAsync(_groupLayout, _groupLayoutFilePath, (success) =>
            {
                if(!success)
                    Debug.LogError($">>> Failed to save {_groupLayoutFilePath}");
            });
        }
        
        private IEnumerator CategorizeSubgraphs()
        {
            int i = 0;
            foreach (var pair in _allSubgraphs)
            {
                i++;
                var hash = pair.Key;
                var subgraph = pair.Value;
                var nodes = subgraph.Nodes;
                var sources = _SubgraphSources[hash];
                bool isShared = sources.Count > 1;
                
                if(nodes.Count == 0)
                    Debug.LogError($"{hash} has no nodes"); 
                
                if(sources.Count == 0)
                    Debug.LogError($"{hash} has no sources"); 

                if (nodes.Count == 1)
                {
                    var singleNode = nodes.ToList()[0];
                    var outgoingEdges = CountOutgoingEdges(singleNode);
                    var incomingEdges = CountIncomingEdges(singleNode);
                    if (incomingEdges == 0 && outgoingEdges == 0)
                    {
                        _singleAssets.Add(hash, subgraph);
                        continue;
                    }

                    if (isShared && outgoingEdges == 0) // consists only of a single sink node
                    {
                        _sharedSingleSinks.Add(hash, subgraph);
                        continue;
                    }
                    
                    if (incomingEdges == 0) // consists only of a single source node
                    {
                        _singleSources.Add(hash, subgraph);
                        continue;
                    }
                }
                else
                {
                    if (sources.IsSubsetOf(nodes))
                    {
                        //Hierarchies always have one source because that source needs to be its own source as well
                        if(sources.Count > 1)
                            Debug.LogError($"{hash} Unknown hierarchy {sources.Count}");
                        
                        _hierarchies.Add(hash, subgraph);
                        continue;
                    }
                }

                if (isShared) //Has more than one source
                {
                    if (nodes.Count == 1)
                        _sharedSingles.Add(hash, subgraph);
                    else
                        _sharedAssets.Add(hash, subgraph);
                    continue;
                }
                else
                {
                    //These are due to cycles and the fact hat cycle nodes doesn't follow the rule of incoming/outgoing node count 
                    _ExclusiveToSingleSource.Add(hash, subgraph);
                }

                if (ShouldUpdateUi)
                {
                    _sequence.ReportProgress((float)i/_allSubgraphs.Count, hash.ToString());
                    yield return null;
                }
            }

            yield break;
        }

        private bool CanMove(int fromHash, int toHash)
        {
            Category fromCategory = null;
            Category toCategory = null;
            
            foreach (var category in _Categories.Values)
            {
                if (category.TryGetValue(fromHash, out var fromSubgraph))
                {
                    fromCategory = category;
                }
                
                if (category.TryGetValue(toHash, out var toSubgraph))
                {
                    toCategory = category;
                }
            }

            if (fromCategory == null || toCategory == null)
                return false;

            if (!fromCategory.CanMoveFrom || !toCategory.CanMoveTo)
                return false;

            var sourcesOfFromSubgraph = _SubgraphSources[fromHash];
            var sourcesOfToSubgraph = _SubgraphSources[toHash];

            if (!sourcesOfFromSubgraph.IsSubsetOf(sourcesOfToSubgraph)) 
                return false;

            return true;
        }

        private IEnumerator MoveSubgraphsByRule(MergeRule rule)
        {
            var moveList = new List<(int, int)>();

            var fromCategory = _Categories[rule.OriginCategory];
            var toCategory = _Categories[rule.DestinationCategory];

            var fromCategorySelection = fromCategory.Where(rule.FromCondition).ToList();
            var toCategorySelection = toCategory.Where(rule.ToCondition).ToList();
            
            //Calculate merge possibility
            foreach (var originPair in fromCategorySelection)
            { 
                var fromHash = originPair.Key;
                
                foreach (var destinationPair in toCategorySelection)
                {
                    var toHash = destinationPair.Key;
                    
                    if (CanMove(fromHash, toHash))
                    {
                        moveList.Add(new ValueTuple<int, int>(fromHash, toHash));
                        break;
                    }
                    
                    if (ShouldUpdateUi)
                        yield return null;
                }
            }

            //Actual moving
            int moveCount = 0;
            foreach (var moveItem in moveList)
            {
                var fromHash = moveItem.Item1;
                var toHash = moveItem.Item2;

                var fromSubgraph = fromCategory[fromHash];
                var toSubgarph = toCategory[toHash];
                
                toSubgarph.Nodes.UnionWith(fromSubgraph.Nodes);
                fromSubgraph.Nodes.Clear();
                fromCategory.Remove(fromHash);
                moveCount++;
                
                if (ShouldUpdateUi)
                    yield return null;
            }

            string result =  $"{rule.OriginCategory} ---({moveCount})---> {rule.DestinationCategory}\n";
            _result += result + "\n";
        }
        
        private IEnumerator AddToGroupLayoutByRule(GroupLayoutRule rule)
        {
            var category = _Categories[rule.CategoryId];
            
            int subgraphProcessed = 0;

            if (category.MergeAllBeforeGrouping)
            {
                var nodes = new List<AssetNode>();
                foreach (var pair in category)
                {
                    // Merge all nodes from all subgraphs into one collection
                    // and map it to a single group
                    var nodesInSubgraph = pair.Value.Nodes;
                    foreach (var node in nodesInSubgraph)
                    {
                        if(nodes.Contains(node))
                        {
                            Debug.LogError("node exists!");
                            continue;
                        }
                        nodes.Add(node);
                    }
                    
                    subgraphProcessed++;
                    if (ShouldUpdateUi)
                    {
                        _sequence.ReportProgress((float)subgraphProcessed/category.Count, $"Processing group layout: {pair.Key}");
                        yield return null;
                    }
                }

                List<List<AssetNode>> nodesSplit = new List<List<AssetNode>>();
                yield return SplitBySize(nodes, rule.MaxSize, nodesSplit);

                int outputCount = nodesSplit.Sum(list => list.Count);
                if (outputCount != nodes.Count)
                {
                    Debug.LogError($"Node splitting {rule.CategoryId}: input({nodes.Count}) output({outputCount}) count mismatch!");
                }

                for (int i = 0; i < nodesSplit.Count; i++)
                {
                    var groupName = $"{rule.CategoryId}_{i}";
            
                    var groupLayoutInfo = new GroupLayoutInfo()
                    {
                        TemplateName = rule.TemplateName,
                        Nodes = nodesSplit[i]
                    };
        
                    if (groupLayoutInfo.Nodes.Count > 0)
                        _groupLayout.Add(groupName, groupLayoutInfo);
                }
            }
            else
            {
                //one subgraph maps to one group
                //
                foreach (var pair in category)
                {
                    var hash = pair.Key;
                    var subgraph = pair.Value;
                    var sources = _SubgraphSources[hash];

                    var groupName = GetSubgraphName(subgraph, sources); //ToDo: Add a naming settings to customize names
                    if (_groupLayout.ContainsKey(groupName))
                    {
                        //If name already registered, switch to fallback name
                        groupName += $"_{hash}";
                    }

                    var groupLayoutInfo = new GroupLayoutInfo()
                    {
                        TemplateName = rule.TemplateName,
                        Nodes = subgraph.Nodes.ToList()
                    };
                    
                    if (groupLayoutInfo.Nodes.Count > 0)
                        _groupLayout.Add(groupName, groupLayoutInfo);

                    subgraphProcessed++;
                    if (ShouldUpdateUi)
                    {
                        _sequence.ReportProgress((float)subgraphProcessed/category.Count, $"Processing group layout: {hash}");
                        yield return null;
                    }
                }
            }
        }

        private void PrintCategories()
        {
            int total = _hierarchies.Count + _singleAssets.Count + _sharedAssets.Count + _sharedSingles.Count + 
                        _sharedSingleSinks.Count + _singleSources.Count + _ExclusiveToSingleSource.Count;
            
            string result = "New Categories: \n";

            result += $"_hierarchies = {_hierarchies.Count} ({(float)_hierarchies.Count / total:P1}) \n";
            result += $"_singleAssets = {_singleAssets.Count} ({(float)_singleAssets.Count / total:P1})\n";
            result += $"_sharedAssets = {_sharedAssets.Count} ({(float)_sharedAssets.Count / total:P1})\n";
            result += $"_sharedSingles = {_sharedSingles.Count} ({(float)_sharedSingles.Count / total:P1})\n";
            result += $"_sharedSingleSinks = {_sharedSingleSinks.Count} ({(float)_sharedSingleSinks.Count / total:P1})\n";
            result += $"_singleSources = {_singleSources.Count} ({(float)_singleSources.Count / total:P1})\n";
            result += $"_others = {_ExclusiveToSingleSource.Count} ({(float)_ExclusiveToSingleSource.Count / total:P1})\n";
            
            _result += result + "\n";
        }
        
        private void Verify()
        {
            var inputNodes = VerifyCategoryCount(_allSubgraphs);

            var hierarchiesCount = VerifyCategoryCount(_hierarchies);
            var singleAssetsCount = VerifyCategoryCount(_singleAssets);
            var sharedAssetsCount = VerifyCategoryCount(_sharedAssets);
            var sharedSinglesCount = VerifyCategoryCount(_sharedSingles);
            var sharedSingleSinksCount = VerifyCategoryCount(_sharedSingleSinks);
            var singleSourcesCount = VerifyCategoryCount(_singleSources);
            var exclusiveToSingleSourceCount = VerifyCategoryCount(_ExclusiveToSingleSource);
            var outputCount_Categories = hierarchiesCount + singleAssetsCount + sharedAssetsCount + sharedSinglesCount +
                               sharedSingleSinksCount + singleSourcesCount + exclusiveToSingleSourceCount;
            
            var output = new HashSet<AssetNode>();
            foreach (var nodes in _groupLayout.Values)
            {
                foreach (var node in nodes.Nodes)
                {
                    if(!output.Add(node))
                        Debug.LogError($"Duplicate Group Layout node {node.FileName}");
                }
            }
            
            _result += $"Verified = {inputNodes == output.Count}, {inputNodes}, {outputCount_Categories}, {output.Count}\n";
            _result += $"Group count = {_groupLayout.Count}";
        }

        private int CountOutgoingEdges(AssetNode node)
        {
            return _dependencyGraph.GetNeighbors(node).Count;
        }
        
        private int CountIncomingEdges(AssetNode node)
        {
            return _transposedGraph.GetNeighbors(node).Count;
        }

        private int VerifyCategoryCount(Category category)
        {
            var output = new HashSet<AssetNode>();
            foreach (var subgraph in category)
            {
                foreach (var node in subgraph.Value.Nodes)
                {
                    output.Add(node);
                }
            }

            return output.Count;
        }

        private IEnumerator SplitBySize(List<AssetNode> input, float sizeThreshold, List<List<AssetNode>> output)
        {
            float size = 0;
            int index = 0;
            output.Add(new List<AssetNode>());
            
            foreach (var node in input)
            {
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(node.AssetPath);
                size += Profiler.GetRuntimeMemorySizeLong(asset) / (1024f * 1024f);
                if (size <= sizeThreshold)
                {
                    output[index].Add(node);
                }
                else
                {
                    size = 0;
                    index++;
                    output.Add(new List<AssetNode>());
                    output[index].Add(node);
                }

                if (ShouldUpdateUi)
                    yield return null;
            }
        }
        
        private void DisplayResultsOnUi()
        {
            if (_uiGroup != null)
                _uiGroup.OutputText = _result;
            Debug.Log($"Group layout created in t={EditorApplication.timeSinceStartup - startTime:F2}");
        }
    }
}
