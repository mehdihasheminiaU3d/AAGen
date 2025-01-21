using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AAGen.Runtime;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace AAGen
{
    /// <summary>
    /// A processor that analyzes dependency graph data and diagnoses potential duplicates of addressable assets
    /// in the built-in scene bundle (the scenes listed in the build settings).
    /// </summary>
    internal class DependencyOverlapDetector : DependencyGraphProcessor
    {
        public DependencyOverlapDetector(DependencyGraph dependencyGraph, EditorUiGroup uiGroup) 
            : base(dependencyGraph, uiGroup) {}
        
        private DependencyGraph _transposedGraph;
        
        private Dictionary<AssetNode,List<AssetNode>> _sceneHierarchies;
        private AddressableAssetSettings _addressableSettings;
        private List<AssetNode> _sceneNodes;
        private string _result;

        private EditorJobGroup _sequence;
        
        private const bool _printShortPaths = true;
        
        public void FindDirectOverlaps()
        {
            _sequence = new EditorJobGroup(nameof(DependencyOverlapDetector));
            _sequence.AddJob(new ActionJob(Initialize, nameof(Initialize)));
            _sequence.AddJob(new CoroutineJob(FindSceneHierarchies, nameof(FindSceneHierarchies)));
            _sequence.AddJob(new CoroutineJob(FindDirectAddressablesInSceneHierarchy, nameof(FindDirectAddressablesInSceneHierarchy)));
            _sequence.AddJob(new ActionJob(Print, nameof(Print)));
            EditorCoroutineUtility.StartCoroutineOwnerless(_sequence.Run());
        }
        
        public void FindIndirectOverlaps()
        {
            _sequence = new EditorJobGroup(nameof(DependencyOverlapDetector));
            _sequence.AddJob(new ActionJob(Initialize, nameof(Initialize)));
            _sequence.AddJob(new CoroutineJob(FindSceneHierarchies, nameof(FindSceneHierarchies)));
            _sequence.AddJob(new CoroutineJob(FindIndirectAddressablesInSceneHierarchy, nameof(FindIndirectAddressablesInSceneHierarchy)));
            _sequence.AddJob(new ActionJob(Print, nameof(Print)));
            EditorCoroutineUtility.StartCoroutineOwnerless(_sequence.Run());
        }

        private void Initialize()
        {
            _addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            if (_addressableSettings == null)
            {
                Debug.LogError("Addressable Asset Settings not found.");
                return;
            }
            
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length == 0)
            {
                Debug.LogError($"No scene is added to build settings");
                return;
            }
            
            _transposedGraph = new DependencyGraph(_dependencyGraph.GetTransposedGraph());
            _sceneHierarchies = new Dictionary<AssetNode, List<AssetNode>>();
            _sceneNodes = scenes.Select(scene => AssetNode.FromAssetPath(scene.path)).ToList();
            _result = null;
        }

        private IEnumerator FindSceneHierarchies()
        {
            foreach (var sceneNode in _sceneNodes)
            {
                var visited = new HashSet<AssetNode>();
                var component = new List<AssetNode>();
                _dependencyGraph.DepthFirstSearchIterative(sceneNode, visited, (currentNode) => component.Add(currentNode));
                _sceneHierarchies.Add(sceneNode, component);
                yield return null;
            }
        }

        private IEnumerator FindDirectAddressablesInSceneHierarchy()
        {
            foreach (var sceneNode in _sceneNodes)
            {
                _result += $"{sceneNode.FileName}:\n \n";
                
                var sceneHierarchy = _sceneHierarchies[sceneNode];
                
                var directDependentAddressables = new List<AssetNode>();
                foreach (var node in sceneHierarchy)
                {
                    if(IsAddressable(node))
                        directDependentAddressables.Add(node);
                }
                
                if (directDependentAddressables.Count > 0)
                {
                    for (var i = 0; i < directDependentAddressables.Count; i++)
                    {
                        var node = directDependentAddressables[i];
                        _result += $"{i + 1} - {node.AssetPath}:\n";
                        var paths = _dependencyGraph.DepthFirstSearchForAllPaths(sceneNode, node);
                        _result += PrintPaths(paths) + "\n";
                    }
                }
                
                yield return null;
            }
        }
        
        private IEnumerator FindIndirectAddressablesInSceneHierarchy()
        {
            foreach (var sceneNode in _sceneNodes)
            {
                _result += $"{sceneNode.FileName}:\n";
                
                _sequence.ReportProgress(0.1f/_sceneNodes.Count, "0"); 
                yield return null;
                
                var sceneSources = FindLeafNodes(_transposedGraph, sceneNode);
                _result += $"Source nodes for {sceneNode.FileName}: {string.Join(", ", sceneSources.Select(src => src.FileName))} \n \n";
                
                var sceneHierarchy = _sceneHierarchies[sceneNode];
                
                _sequence.ReportProgress(0.4f/_sceneNodes.Count, "1"); 
                yield return null;

                var breachingNodes = new Dictionary<AssetNode, HashSet<AssetNode>>();
                foreach (var node in sceneHierarchy)
                {
                    if (IsAddressable(node))
                        continue;
                        
                    var nodeSources = FindLeafNodes(_transposedGraph, node);
                    if(!sceneSources.SetEquals(nodeSources))
                    {
                        var addressableSources = new HashSet<AssetNode>();
                        foreach (var source in nodeSources)
                        {
                            if (IsAddressable(source))
                                addressableSources.Add(source);
                        }
                        breachingNodes.Add(node, addressableSources);
                    }
                    yield return null;
                }
                
                _sequence.ReportProgress(0.8f/_sceneNodes.Count, "2");
                yield return null;
                
                if (breachingNodes.Count > 0)
                {
                    int nodeCount = 0;
                    foreach (var kvp in breachingNodes)
                    {
                        nodeCount++;
                        var node = kvp.Key;
                        var nodeSources = kvp.Value;
                        
                        _result += $"{nodeCount} - {node.AssetPath}:\n";

                        foreach (var source in nodeSources)
                        {
                            if (_printShortPaths)
                            {
                                _result += $"{source.FileName} -> ... -> {node.FileName} \n";
                            }
                            else
                            {
                                var paths = _dependencyGraph.DepthFirstSearchForAllPaths(source, node);
                                _result += PrintPaths(paths) + "\n"; 
                            }
                            
                            yield return null;
                        }

                        _result += $"\n";
                        yield return null;
                    }
                }
                
                //for each scene
                yield return null;
            }
        }

        private void Print()
        {
            _uiGroup.OutputText = _result;
            _transposedGraph = null;
            _sceneHierarchies = null;
        }

        private bool IsAddressable(AssetNode node)
        {
            return _addressableSettings.FindAssetEntry(node.Guid.ToString()) != null;
        }
    }
}
