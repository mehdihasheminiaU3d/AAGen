using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AAGen.Shared
{
    /// <summary>
    /// Represents a graph structure for managing and processing nodes and edges.
    /// By default, the implementation represents a directed graph. However, some concepts
    /// are only applicable to an undirected graph and are explicitly mentioned in the method names.
    /// </summary>
    [Serializable]
    public class Graph<T> where T:IEquatable<T>
    {
        [JsonRequired]
        protected Dictionary<T, List<T>> _adjacencyList;

        [JsonIgnore]
        public int NodeCount => _adjacencyList.Count;

        public Graph()
        {
            _adjacencyList = new Dictionary<T, List<T>>();
        }
        
        public virtual void AddEdge(T u, T v)
        {
            AddNode(u);
            AddNode(v);
            _adjacencyList[u].Add(v);
        }

        public virtual void AddNode(T node)
        {
            if (!_adjacencyList.ContainsKey(node))
            {
                _adjacencyList[node] = new List<T>();
            }
        }

        public List<T> GetNeighbors(T node)
        {
            return _adjacencyList.TryGetValue(node, out var neighbors) ? neighbors : new List<T>();
        }

        public List<T> GetAllNodes()
        {
            return new List<T>(_adjacencyList.Keys);
        }
        
        public Graph<T> GetTransposedGraph()
        {
            var transposedGraph = new Graph<T>();
            
            foreach (var node in _adjacencyList)
            {
                T fromNode = node.Key;

                foreach (var toNode in node.Value)
                {
                    // Reverse the edge (add an edge from 'toNode' to 'fromNode')
                    transposedGraph.AddEdge(toNode, fromNode);
                }
            }

            return transposedGraph;
        }
        
        public void DepthFirstSearch(T startNode, Action<T> onVisit)
        {
            HashSet<T> visited = new HashSet<T>();
            DepthFirstSearchIterative(startNode, visited, onVisit);
        }

        public void DepthFirstSearchIterative(T startNode, HashSet<T> visited, Action<T> onVisit)
        {
            Stack<T> stack = new Stack<T>();
            stack.Push(startNode);
    
            while (stack.Count > 0)
            {
                // Pop a vertex from the stack
                T node = stack.Pop();

                // If the node hasn't been visited yet
                if (!visited.Contains(node))
                {
                    visited.Add(node);  // Mark the node as visited
                    
                    onVisit?.Invoke(node);

                    // Push all unvisited neighbors to the stack
                    foreach (var neighbor in GetNeighbors(node))
                    {
                        if (!visited.Contains(neighbor))
                        {
                            stack.Push(neighbor);
                        }
                    }
                }
            }
        }
        
        public List<List<T>> DepthFirstSearchForAllPaths(T startNode, T endNode)
        {
            // List to store all paths
            List<List<T>> allPaths = new List<List<T>>();
        
            // Stack stores the current node and the path leading to it
            Stack<Tuple<T, List<T>>> stack = new Stack<Tuple<T, List<T>>>();
        
            // Initialize the stack with the start node and an empty path
            stack.Push(Tuple.Create(startNode, new List<T> { startNode }));

            // Perform DFS
            while (stack.Count > 0)
            {
                var (currentNode, currentPath) = stack.Pop();

                // If we reached the end node, add the current path to the result
                if (EqualityComparer<T>.Default.Equals(currentNode, endNode))
                {
                    allPaths.Add(new List<T>(currentPath));
                }

                // Continue the DFS for each neighbor
                foreach (var neighbor in GetNeighbors(currentNode))
                {
                    if (!currentPath.Contains(neighbor))  // Avoid revisiting nodes in the current path
                    {
                        // Add neighbor to the path and push the new path onto the stack
                        List<T> newPath = new List<T>(currentPath) { neighbor };
                        stack.Push(Tuple.Create(neighbor, newPath));
                    }
                }
            }

            return allPaths; 
        }
        
        public List<List<T>> DepthFirstSearchForAllPaths(T startNode, Func<T,bool> endNodeCondition)
        {
            // List to store all paths
            List<List<T>> allPaths = new List<List<T>>();
        
            // Stack stores the current node and the path leading to it
            Stack<Tuple<T, List<T>>> stack = new Stack<Tuple<T, List<T>>>();
        
            // Initialize the stack with the start node and an empty path
            stack.Push(Tuple.Create(startNode, new List<T> { startNode }));

            // Perform DFS
            while (stack.Count > 0)
            {
                var (currentNode, currentPath) = stack.Pop();

                // If we reached the end node, add the current path to the result
                if (endNodeCondition(currentNode))
                {
                    allPaths.Add(new List<T>(currentPath));
                }

                // Continue the DFS for each neighbor
                foreach (var neighbor in GetNeighbors(currentNode))
                {
                    if (!currentPath.Contains(neighbor))  // Avoid revisiting nodes in the current path
                    {
                        // Add neighbor to the path and push the new path onto the stack
                        List<T> newPath = new List<T>(currentPath) { neighbor };
                        stack.Push(Tuple.Create(neighbor, newPath));
                    }
                    else
                    {
                        allPaths.Add(new List<T>(currentPath));
                    }
                }
            }

            return allPaths; 
        }

        /// <summary>
        /// Breadth-First Search (BFS)
        /// </summary>
        public void BreadthFirstSearch(T startNode, Action<T> visit)
        {
            HashSet<T> visited = new HashSet<T>();
            Queue<T> queue = new Queue<T>();

            // Mark the start node as visited and enqueue it
            visited.Add(startNode);
            queue.Enqueue(startNode);

            while (queue.Count > 0)
            {
                // Dequeue a vertex from the queue and process it
                T node = queue.Dequeue();
                visit(node);

                // Get all the neighbors of the dequeued node
                foreach (var neighbor in GetNeighbors(node))
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }
        
        public Graph<TResult> ConvertNodeType<TResult>(Func<T, TResult> converter) where TResult:IEquatable<TResult>
        { 
            var destinationAdjacencyList = new Dictionary<TResult, List<TResult>>();
            
            foreach (var kvp in _adjacencyList)
            {
                TResult node = converter(kvp.Key);
                List<TResult> neighbors = new List<TResult>();
                
                foreach (var neighbor in kvp.Value)
                {
                    neighbors.Add(converter(neighbor));
                }
                
                destinationAdjacencyList.Add(node, neighbors);
            }

            return new Graph<TResult>
            {
                _adjacencyList = destinationAdjacencyList
            };
        }
        
        public Graph<T> GetSubgraph(List<T> nodes)
        {
            Dictionary<T, List<T>> subgraphAdjacencyList = new Dictionary<T, List<T>>();
            
            foreach (T node in nodes)
            {
                if (_adjacencyList.TryGetValue(node, out var sourceNeighbors))
                {
                    List<T> neighbors = sourceNeighbors.Where(nodes.Contains).ToList();
                    subgraphAdjacencyList.Add(node, neighbors);
                }
            }
            
            return new Graph<T>
            {
                _adjacencyList = subgraphAdjacencyList
            };
        }
        
        public static Graph<T> ToUndirected(Graph<T> directedGraph)
        {
            Graph<T> undirectedGraph = new Graph<T>();

            foreach (var node in directedGraph._adjacencyList.Keys)
            {
                foreach (var neighbor in directedGraph._adjacencyList[node])
                {
                    undirectedGraph.AddEdge(node, neighbor); 
                    undirectedGraph.AddEdge(neighbor, node); 
                }
            }

            return undirectedGraph;
        }
        
        public static void RemoveNodeFromUndirectedGraph(Graph<T> undirectedGraph, T targetNode)
        {
            // Gets the list of neighbors from undirected graph to
            // cover both incoming edges and outgoing edges
            var neighbors = undirectedGraph.GetNeighbors(targetNode);
            
            if (neighbors.Count > 0)
            {
                // Remove the edge to and from the target node
                foreach (var neighbor in neighbors)
                {
                    var neighborsOfNeighbor = undirectedGraph.GetNeighbors(neighbor);
                    if (neighborsOfNeighbor.Count > 0)
                        neighborsOfNeighbor.Remove(targetNode);
                }
            }
            
            // Removes node and the list of neighbors
            undirectedGraph._adjacencyList.Remove(targetNode); 
        }
        
        public static List<List<T>> GetConnectedComponentsOfUndirectedGraph(Graph<T> undirectedGraph)
        {
            var nodes = undirectedGraph.GetAllNodes();
            var visited = new HashSet<T>();
            List<List<T>> components = new List<List<T>>();

            // Iterate over all vertices
            foreach (var node in nodes)
            {
                // If the vertex has not been visited, it's a new component
                if (!visited.Contains(node))
                {
                    var component = new List<T>();
                    undirectedGraph.DepthFirstSearchIterative(node, visited, (currentNode) => component.Add(currentNode));
                    components.Add(component);
                }
            }

            return components;
        }
    }
}
