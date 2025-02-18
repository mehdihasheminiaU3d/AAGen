using System.Collections.Generic;
using AAGen.Shared;
using UnityEditor;

namespace AAGen.AssetDependencies
{
    /// <summary>
    /// A graph that encapsulates asset relationship data.
    /// </summary>
    internal class DependencyGraph : Graph<AssetNode>
    {
        public DependencyGraph() {}

        public DependencyGraph(Graph<AssetNode> graph)
        {
            FromGraph(graph);
        }

        public void AddEdge(string pathA, string pathB)
        {
            //Convert path to guid
            var guidA = AssetDatabase.GUIDFromAssetPath(pathA);
            var guidB = AssetDatabase.GUIDFromAssetPath(pathB);

            base.AddEdge(new AssetNode(guidA), new AssetNode(guidB));
        }
        
        public void AddNode(string path)
        {
            //Convert path to guid
            var guid = AssetDatabase.GUIDFromAssetPath(path);
            base.AddNode(new AssetNode(guid));
        }

        private void FromGraph(Graph<AssetNode> graph)
        {
            _adjacencyList = new Dictionary<AssetNode, List<AssetNode>>();
            foreach (var node in graph.GetAllNodes())
            {
                _adjacencyList.Add(node, graph.GetNeighbors(node));
            }
        }
    }
}
