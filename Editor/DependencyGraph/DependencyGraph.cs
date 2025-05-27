using System;
using System.Collections.Generic;
using AAGen.Shared;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace AAGen.AssetDependencies
{
    /// <summary>
    /// A graph that encapsulates asset relationship data.
    /// </summary>
    public class DependencyGraph : Graph<AssetNode>
    {
        public DependencyGraph()
        {
            m_LazyTransposedGraph = new Lazy<DependencyGraph>(() => new DependencyGraph(GetTransposedGraph()));
        }
        
        public DependencyGraph(Graph<AssetNode> graph)
        {
            FromGraph(graph);
            m_LazyTransposedGraph = new Lazy<DependencyGraph>(() => new DependencyGraph(GetTransposedGraph()));
        }
        
        readonly Lazy<DependencyGraph> m_LazyTransposedGraph;
        
        [JsonIgnore]
        public DependencyGraph Transposed => m_LazyTransposedGraph.Value;

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
        
        public int CountOutgoingEdges(AssetNode node)
        {
            return GetNeighbors(node).Count;
        }
            
        public int CountIncomingEdges(AssetNode node)
        {
            return Transposed.GetNeighbors(node).Count;
        }
        
        public bool IsSourceNode(AssetNode node)
        {
            return CountIncomingEdges(node) == 0;
        }
        
        public bool IsSinkNode(AssetNode node)
        {
            return CountOutgoingEdges(node) == 0;
        }

        private void FromGraph(Graph<AssetNode> graph)
        {
            _adjacencyList = new Dictionary<AssetNode, List<AssetNode>>();
            foreach (var node in graph.GetAllNodes())
            {
                _adjacencyList.Add(node, graph.GetNeighbors(node));
            }
        }

        [Serializable]
        public class SerializedData
        {
            public Graph<int> Graph = new Graph<int>();
            public Dictionary<string, int> IndexDictionary = new Dictionary<string, int>();
        }
        
        public SerializedData Serialize()
        {
            var serializedData = new SerializedData();
            int index = 0;
            
            serializedData.Graph = ConvertNodeType(ConvertGuidToIndex);
            return serializedData;
            
            int ConvertGuidToIndex(AssetNode node)
            {
                var guidString = node.Guid.ToString();
                if (serializedData.IndexDictionary.TryGetValue(guidString, out var recordedIndex))
                {
                    return recordedIndex;
                }

                index++;
                serializedData.IndexDictionary.Add(guidString, index);

                return index;
            }
        }

        public static DependencyGraph Deserialize(SerializedData serializedData)
        {
            var invertedDictionary = new Dictionary<int, string>();
            foreach (var kvp in serializedData.IndexDictionary)
            {
                invertedDictionary.Add(kvp.Value, kvp.Key);
            }
            
            var graph = serializedData.Graph.ConvertNodeType(ConvertIndexToGuid);

            return new DependencyGraph(graph);
            
            AssetNode ConvertIndexToGuid(int nodeIndex)
            {
                var guidString = invertedDictionary[nodeIndex];
                return AssetNode.FromGuidString(guidString);
            }
        }
    }
}
