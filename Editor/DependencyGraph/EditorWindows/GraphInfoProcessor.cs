using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AAGen.Runtime;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace AAGen
{
    /// <summary>
    /// A processor that analyzes dependency graph data and extracts useful information from it.
    /// </summary>
    internal class GraphInfoProcessor : DependencyGraphProcessor
    {
        public GraphInfoProcessor(DependencyGraph dependencyGraph, EditorUiGroup uiGroup) 
            : base(dependencyGraph, uiGroup){}
        
        private List<List<AssetNode>> _components;
        private string _result;

        private DependencyGraph _transposedGraph;
        private EditorJobGroup _sequence;
        
        public void FindConnectedComponents()
        {
            _sequence = new EditorJobGroup(nameof(GraphInfoProcessor));
            _sequence.AddJob(new ActionJob(Init, nameof(Init)));
            _sequence.AddJob(new CoroutineJob(GetConnectedComponentsInGraph, nameof(GetConnectedComponentsInGraph)));
            _sequence.AddJob(new ActionJob(Print, nameof(Print)));
            EditorCoroutineUtility.StartCoroutineOwnerless(_sequence.Run());
        }
        
        public void FindSourceNodes()
        {
            _sequence = new EditorJobGroup(nameof(FindSourceNodes));
            _sequence.AddJob(new ActionJob(Init, nameof(Init)));
            _sequence.AddJob(new CoroutineJob(FindSourceNodesForAsset, nameof(FindSourceNodesForAsset)));
            _sequence.AddJob(new ActionJob(Print, nameof(Print)));
            EditorCoroutineUtility.StartCoroutineOwnerless(_sequence.Run());
        }
        
        public void FindSinkNodes()
        {
            _sequence = new EditorJobGroup(nameof(FindSinkNodes));
            _sequence.AddJob(new ActionJob(Init, nameof(Init)));
            _sequence.AddJob(new CoroutineJob(FindSinkNodesForAsset, nameof(FindSinkNodesForAsset)));
            _sequence.AddJob(new ActionJob(Print, nameof(Print)));
            EditorCoroutineUtility.StartCoroutineOwnerless(_sequence.Run());
        }
        
        public void FindPaths()
        {
            _sequence = new EditorJobGroup(nameof(FindPaths));
            _sequence.AddJob(new ActionJob(Init, nameof(Init)));
            _sequence.AddJob(new CoroutineJob(FindPathsBetweenAssets, nameof(FindPathsBetweenAssets)));
            _sequence.AddJob(new ActionJob(Print, nameof(Print)));
            EditorCoroutineUtility.StartCoroutineOwnerless(_sequence.Run());
        }

        protected override void Init()
        {
            _result = null;
            _transposedGraph = new DependencyGraph(_dependencyGraph.GetTransposedGraph());
        }

        private IEnumerator GetConnectedComponentsInGraph()
        {
            var undirectedGraph = Graph<AssetNode>.ToUndirected(_dependencyGraph);
            _components = Graph<AssetNode>.GetConnectedComponentsOfUndirectedGraph(undirectedGraph);
            
            _result = $"{_components.Count} connected components\n";
            for (var i = 0; i < _components.Count; i++)
            {
                var component = _components[i];
                _result += $"Component{i} includes {component.Count} nodes";
                _result += $" : {string.Join(", ", component.Select(node => node.FileName))}"; 
                _result += "\n";
            }
            
            yield break;
        }
        
        private IEnumerator FindSourceNodesForAsset()
        {
            var targetAsset = AssetDatabase.GetAssetPath(_uiGroup.ObjectInput1);
            if (string.IsNullOrEmpty(targetAsset))
            {
                Debug.LogError($"Asset cannot be found!");
                yield break;
            }

            var targetNode = AssetNode.FromAssetPath(targetAsset);
            if (targetNode == null)
            {
                Debug.LogError($"Node cannot be found!");
                yield break;
            }
            
            var sourceNodes = FindLeafNodes(_transposedGraph, targetNode);
            _result += $"Sources of {targetNode.AssetPath}({sourceNodes.Count}):\n \n";

            int count = 0;
            foreach (var source in sourceNodes)
            {
                count++;
                _result += $"{count} - {source.AssetPath}:\n";
                var paths = _dependencyGraph.DepthFirstSearchForAllPaths(source, targetNode);
                _result += PrintPaths(paths) + "\n";
                
                _sequence.ReportProgress((float)count/sourceNodes.Count, $"Processing paths from {source.FileName}");
                yield return null;
            }
        }
        
        private IEnumerator FindSinkNodesForAsset()
        {
            var targetAsset = AssetDatabase.GetAssetPath(_uiGroup.ObjectInput1);
            if (string.IsNullOrEmpty(targetAsset))
            {
                Debug.LogError($"Asset cannot be found!");
                yield break;
            }

            var targetNode = AssetNode.FromAssetPath(targetAsset);
            if (targetNode == null)
            {
                Debug.LogError($"Node cannot be found!");
                yield break;
            }
            
            var sinkNodes = FindLeafNodes(_dependencyGraph, targetNode);
            _result += $"Sink Nodes of {targetNode.AssetPath}({sinkNodes.Count}):\n \n";

            int count = 0;
            foreach (var sinkNode in sinkNodes)
            {
                count++;
                _result += $"{count} - {sinkNode.AssetPath}:\n";
                var paths = _dependencyGraph.DepthFirstSearchForAllPaths(targetNode, sinkNode);
                _result += PrintPaths(paths) + "\n";
                
                _sequence.ReportProgress((float)count/sinkNodes.Count, $"Processing paths from {sinkNode.FileName}");
                yield return null;
            }
        }

        private IEnumerator FindPathsBetweenAssets()
        {
            var fromAsset = AssetDatabase.GetAssetPath(_uiGroup.ObjectInput1);
            var toAsset = AssetDatabase.GetAssetPath(_uiGroup.ObjectInput2);
            if (string.IsNullOrEmpty(fromAsset) || string.IsNullOrEmpty(toAsset))
            {
                Debug.LogError($"Asset cannot be found!");
                yield break;
            }

            var fromNode = AssetNode.FromAssetPath(fromAsset);
            var toNode = AssetNode.FromAssetPath(toAsset);
            if (fromNode == null || toNode == null)
            {
                Debug.LogError($"Node cannot be found!");
                yield break;
            }

            var paths = _dependencyGraph.DepthFirstSearchForAllPaths(fromNode, toNode);
            if(paths.Count>0)
            {
                _result += $"All paths from {fromNode.AssetPath} to {toNode.AssetPath} ({paths.Count}):\n \n";
                _result += PrintPaths(paths) + "\n";
            }
            else
            {
                _result += $"No paths from {fromNode.AssetPath} to {toNode.AssetPath}";
            }
        }

        private void Print()
        {
            _uiGroup.OutputText = _result;
            _transposedGraph = null;
        }
    }
}
