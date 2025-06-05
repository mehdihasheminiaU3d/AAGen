using Codice.Client.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using static Codice.CM.Common.CmCallContext;

namespace AAGen.Shared
{
    /// <summary>
    /// Represents a graph structure for managing and processing nodes and edges.
    /// </summary>
    /// <remarks>
    /// By default, the implementation represents a directed graph. However, some concepts
    /// are only applicable to an undirected graph and are explicitly mentioned in the method names.
    /// </remarks>
    [Serializable]
    public class Graph<T> where T:IEquatable<T>
    {
        #region Fields
        /// <summary>
        /// A collection of nodes that are have edges from the associated node.
        /// </summary>
        [JsonRequired]
        protected Dictionary<T, List<T>> _adjacencyList;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the number of nodes in the graph.
        /// </summary>
        [JsonIgnore]
        public int NodeCount => _adjacencyList.Count;
        #endregion

        #region Methods
        /// <summary>
        /// Create a new instance of the <see cref="Graph{T}"/> class.
        /// </summary>
        public Graph()
        {
            // Create a new instance of the map.
            _adjacencyList = new Dictionary<T, List<T>>();
        }

        /// <summary>
        /// Gets a value indicating whether the node exists in the graph.
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <returns>A value indicating whether the node exists in the graph.</returns>
        public bool Exists(T node)
        {
            // If the node node does not exist in the graph.
            return _adjacencyList.ContainsKey(node);
        }

        /// <summary>
        /// Adds a neighboring edge from one node to the other.
        /// </summary>
        /// <param name="fromNode">The node that the edge starts at.</param>
        /// <param name="toNode">The node that the edge ends at.</param>
        public virtual void AddEdge(T fromNode, T toNode)
        {
            /// Ensure that the node that is the source and destination of the edge
            /// are in the graph by placing them in the graph if they aren't already.
            AddNode(fromNode);
            AddNode(toNode);

            // Retrieve the list of adjacent nodes that are associated the node that is the source of the edge.
            // Add the edge to the list of 
            _adjacencyList[fromNode].Add(toNode);
        }
        
        /// <summary>
        /// Adds a node to the graph.
        /// </summary>
        /// <param name="node">The node to add in the graph.</param>
        public virtual void AddNode(T node)
        {
            // If the node does not already exist in the graph, then:
            if (!Exists(node))
            {
                // Create a new empty list of adjacent nodes and associate them with the node.
                // This defines the node is a member of the graph and that is has no edges to or from it.
                _adjacencyList[node] = new List<T>();
            }
        }

        /// <summary>
        /// Gets a collection of nodes that are neighboring (aka have an edge from) the given node.
        /// </summary>
        /// <param name="node">A node which may have neigboring nodes.</param>
        /// <returns>A collection of nodes that are neighboring from the givenm node. If there are no neighboring nodes, then the collection is empty.</returns>
        public List<T> GetNeighbors(T node)
        {
            return _adjacencyList.TryGetValue(node, out var neighbors) ? neighbors : new List<T>();
        }

        /// <summary>
        /// Gets a list of nodes that exist in the graph.
        /// </summary>
        /// <returns>The list of nodes that exist in the graph.</returns>
        public List<T> GetAllNodes()
        {
            return new List<T>(_adjacencyList.Keys);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Graph{T}"/> class that is a transpose of this graph (aka edges reversed).
        /// </summary>
        /// <returns>A new instance of the <see cref="Graph{T}"/> class that is a transpose of this graph.</returns>
        // NOTE: could be made a purely functional (aka static) member.
        public Graph<T> GetTransposedGraph()
        {
            // Create a new instance of a graph.
            var transposedGraph = new Graph<T>();
            
            // For every node in this graph, perform the following:
            foreach (var node in _adjacencyList)
            {
                // Get the node in the graph.
                T fromNode = node.Key;

                // For every neighbor that is adjacent to the vode, perform the following:
                foreach (var toNode in node.Value)
                {
                    // Add an edge to the node from the neighbor in the new graph,
                    // essentially adding the nodes to the graph with reversed the edges.
                    transposedGraph.AddEdge(toNode, fromNode);
                }
            }

            // Return the completed transpose graph.
            return transposedGraph;
        }

        /// <summary>
        /// Attempt a depth-first search.
        /// </summary>
        /// <param name="startNode">The node to use as the start point of a search.</param>
        /// <param name="onVisit">The action to perform for each node in the subgraph upon the initial visit.</param>
        // NOTE: could be made a purely functional (aka static) member.
        // NOTE: could be made more generic so that it can do most of the business logic for DepthFirstSearchForAllPaths().
        public void DepthFirstSearch(T startNode, Action<T> onVisit)
        {
            // Create a set of unique nodes to retain state of previously visited nodes during depth-first search.
            var visited = new HashSet<T>();

            // Attempt a depth-first search.
            DepthFirstSearchIterative(startNode, visited, onVisit);
        }

        /// <summary>
        /// Performs a depth first search on the starting node passed in.
        /// </summary>
        /// <param name="startNode">The start node of the graph.</param>
        /// <param name="visited">A set of unique nodes to retain state of previously visited nodes during DFS.</param>
        /// <param name="onVisit">The action to perform for each node in the subgraph upon the initial visit.</param>
        // NOTE: could be made a purely functional (aka static) member.
        public void DepthFirstSearchIterative(T startNode, HashSet<T> visited, Action<T> onVisit)
        {
            // Create a stack, which is the ideal LIFO structure for retaining state of which nodes to process during an a iterative DFS search.
            var stack = new Stack<T>();

            // Push the starting node into the stack.
            // By doing so we create the initial set of nodes to attempt to visit.
            stack.Push(startNode);
            
            // While the stack has nodes to attempt to visit, perform the following:
            while (stack.Count > 0)
            {
                // Pop the next node from the stack to get the next appropriate item to visit.
                T node = stack.Pop();

                // If the node hasn't been visited yet, then:
                // NOTE: we are already checking this below; is there a situation where onVisit?.Invoke causes an early visitation?
                if (!visited.Contains(node))
                {
                    // Mark the node as visited.
                    visited.Add(node);
                    
                    // Process the visit for this node.
                    onVisit?.Invoke(node);

                    // For every neighbor that this node has, perform the following:
                    foreach (var neighbor in GetNeighbors(node))
                    {
                        // If the neighbor has not been visited, then:
                        // NOTE: we are already checking this above; is there a situation where onVisit?.Invoke causes an early visitation?
                        if (!visited.Contains(neighbor))
                        {
                            // Push the unvisited neighbor to the stack.
                            stack.Push(neighbor);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Performs a depth first search in order to find all valid paths in the graph that start at the starting node and end at the end node.
        /// </summary>
        /// <param name="startNode">The node to use as the start point of a path.</param>
        /// <param name="endNode">The node to use as the end point of a path.</param>
        /// <returns>A list of valid paths from the start node to the end node.</returns>
        // NOTE: could be made a purely functional (aka static) member.
        public List<List<T>> DepthFirstSearchForAllPaths(T startNode, T endNode)
        {
            Predicate<T> endNodeCondition = (T currentNode) => EqualityComparer<T>.Default.Equals(currentNode, endNode);

            return DepthFirstSearchForAllPaths(startNode, endNodeCondition); 
        }

        /// <summary>
        /// Performs a depth first search in order to find all valid paths in the graph that start at the starting node and end at the end node.
        /// </summary>
        /// <param name="startNode">The node to use as the start point of a path.</param>
        /// <param name="endNodeCondition">A predicate that handles signifying the end of a path.</param>
        /// <returns>A list of valid paths from the start node to the end node.</returns>
        // NOTE: could be made a purely functional (aka static) member.
        public List<List<T>> DepthFirstSearchForAllPaths(T startNode, Predicate<T> endNodeCondition)
        {
            // List to store all paths
            var allPaths = new List<List<T>>();

            // Create a stack, which is the ideal LIFO structure for retaining state of which nodes to process during an a iterative DFS search.
            var stack = new Stack<Tuple<T, List<T>>>();

            // Push the starting node into the stack along with the path that lead us to it from the starting node, which is itself.
            // By doing so we create the initial set of nodes to attempt to visit.
            stack.Push(Tuple.Create(startNode, new List<T> { startNode }));

            // While the stack has nodes to attempt to visit, perform the following:
            while (stack.Count > 0)
            {
                // Pop the next node from the stack to get the next appropriate item to visit.
                var (currentNode, currentPath) = stack.Pop();

                // If we reached the end condition, then:
                if (endNodeCondition(currentNode))
                {
                    // Add the current path to the result.
                    allPaths.Add(new List<T>(currentPath));
                }

                // For every neighbor that this node has, perform the following:
                foreach (var neighbor in GetNeighbors(currentNode))
                {
                    // If the neighbor has not been visited, then:
                    if (!currentPath.Contains(neighbor))
                    {
                        // Create a copy of the current path and append the neighbor to it as the next node in the path.
                        var newPath = new List<T>(currentPath) { neighbor };

                        // Push the unvisited neighbor to the stack and the path that led us to it from the starting node.
                        stack.Push(Tuple.Create(neighbor, newPath));
                    }
                    else
                    {
                        // Otherwise, the neighbor has been visited.

                        // Add the current path to the result.
                        allPaths.Add(new List<T>(currentPath));
                    }
                }
            }

            return allPaths;
        }

        /// <summary>
        /// Attempt a breadth-first search.
        /// </summary>
        /// <param name="startNode">The node to use as the start point of a search.</param>
        /// <param name="onVisit">The action to perform for each node in the subgraph upon the initial visit.</param>
        // NOTE: could be made a purely functional (aka static) member.
        public void BreadthFirstSearch(T startNode, Action<T> onVisit)
        {
            // Create a set of unique nodes to retain state of previously visited nodes during breadth-first search.
            var visited = new HashSet<T>();

            // Create a queue, which is the ideal FIFO structure for retaining state of which nodes to process during an a iterative BFS search.
            var queue = new Queue<T>();

            // Mark the start node as visited and enqueue it
            visited.Add(startNode);

            // Push the starting node into the queue.
            // By doing so we create the initial set of nodes to attempt to visit.
            queue.Enqueue(startNode);

            // While the queue has nodes to attempt to visit, perform the following:
            while (queue.Count > 0)
            {
                // Dequeue the next node from the queue to get the next appropriate item to visit.
                T node = queue.Dequeue();

                // Process the visit for this node.
                onVisit?.Invoke(node);

                // For every neighbor that this node has, perform the following:
                foreach (var neighbor in GetNeighbors(node))
                {
                    // If the neighbor has not been visited, then:
                    if (!visited.Contains(neighbor))
                    {
                        // NOTE: this adds to visitation at a different place than DFS. should they be consistent?
                        visited.Add(neighbor);

                        // Ebqueue the unvisited neighbor to the queue.
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        /// <summary>
        /// Transforms a <see cref="Graph{T}"/> with nodes of type <typeparamref name="T"/> to type <typeparamref name="TResult"/> while maintaining its edges.
        /// </summary>
        /// <typeparam name="TResult">The new node type.</typeparam>
        /// <param name="converter">The function object that works to convert type <typeparamref name="T"/> to type <typeparamref name="TResult"/>.</param>
        /// <returns>A <see cref="Graph{TResult}"/>.</returns>
        // NOTE: could be made a purely functional (aka static) member.
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
        
        /// <summary>
        /// Create a subgraph of nodes and edges limited to a set of nodes.
        /// </summary>
        /// <param name="subgraphNodes">A list of nodes in the subgraph.</param>
        /// <returns>A new instance of <see cref="Graph{T}"/> that reporesents the subgraph.</returns>
        public Graph<T> GetSubgraph(List<T> subgraphNodes)
        {
            // Create an map of adjacency lists associated with nodes for the subgraph.
            var subgraphAdjacencyList = new Dictionary<T, List<T>>();
            
            // For every node in a list of nodes, perform the following:
            foreach (T node in subgraphNodes)
            {
                // Attempt to get the adjacency list associated with the node. 
                // If the node has an adjacencty list associated, then it exists in the graph.
                // If the node exists in the graph, then:
                if (_adjacencyList.TryGetValue(node, out var sourceNeighbors))
                {
                    // Trim down the adjacency list so that it only contains nodes that are in the subgraph list.
                    List<T> neighbors = sourceNeighbors.Where(subgraphNodes.Contains).ToList();

                    // Associate the trimmed adjacency list with the the node in the map. 
                    subgraphAdjacencyList.Add(node, neighbors);
                }
            }
            
            // Pack the adjacency list into an instance of the subgraph.
            return new Graph<T>
            {
                _adjacencyList = subgraphAdjacencyList
            };
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// Create a graph of nodes where the edges have no direction.
        /// </summary>
        /// <param name="directedGraph">The graph, which should have at least one directed edge.</param>
        /// <returns>A graph of nodes where the edges have no direction.</returns>
        public static Graph<T> ToUndirected(Graph<T> directedGraph)
        {
            // Create a new instance of a graph which represents the undirected graph.
            Graph<T> undirectedGraph = new Graph<T>();

            // For every node in the graph, perform the following:
            foreach (var node in directedGraph._adjacencyList.Keys)
            {
                // For every neighboring node of the node, perform the following:
                foreach (var neighbor in directedGraph._adjacencyList[node])
                {
                    // Create edges between the neighbors that goes in both directions.
                    undirectedGraph.AddEdge(node, neighbor); 
                    undirectedGraph.AddEdge(neighbor, node); 
                }
            }

            return undirectedGraph;
        }

        /// <summary>
        /// Remove a node from an undirected graph.
        /// </summary>
        /// <param name="undirectedGraph">The graph, which is assumed to have all edges undirected.</param>
        /// <param name="targetNode">The node to remove from the graph.</param>
        public static void RemoveNodeFromUndirectedGraph(Graph<T> undirectedGraph, T targetNode)
        {
            // Gets the list of adjacent neighbors that have edges from the target node.
            var neighbors = undirectedGraph.GetNeighbors(targetNode);
            
            // If there are neighbors with edges coming from this node, then:
            if (neighbors.Count > 0)
            {
                // For every neighbor associated with the node, perform the following:
                foreach (var neighbor in neighbors)
                {
                    // Attempt to get the list of adjacency neighbors for the neighbor.
                    var neighborsOfNeighbor = undirectedGraph.GetNeighbors(neighbor);

                    // If there are neighbors with edges coming from this neighbor, then:
                    if (neighborsOfNeighbor.Count > 0)
                    {
                        // Remove only the target node from the the adjacency list, eliminating one direction.
                        neighborsOfNeighbor.Remove(targetNode);
                    }
                }
            }
            
            // The neighbors have eliminated the edge connecting them one way.

            // Removes the node and the edges connecting it to its neighbors in the other direction. 
            undirectedGraph._adjacencyList.Remove(targetNode); 
        }

        /// <summary>
        /// Identifies all sections of an undirected graph that have at least one contiguous path through them.
        /// </summary>
        /// <param name="undirectedGraph">The graph, which is assumed to have all edges undirected.</param>
        /// <returns>The list of components in graph.</returns>
        public static List<List<T>> GetConnectedComponentsOfUndirectedGraph(Graph<T> undirectedGraph)
        {
            // Get a list of all nodes that exist in the graph.
            var nodes = undirectedGraph.GetAllNodes();

            // Create a set of unique nodes to retain state of previously visited nodes during depth-first search.
            var visited = new HashSet<T>();
            
            var components = new List<List<T>>();

            // For all nodes in the graph. perform the following:
            foreach (var node in nodes)
            {
                // If the vertex has not been visited, then:
                if (!visited.Contains(node))
                {
                    // The node exists as part of a new component, list of nodes that have at least one contiguous path through them.
                    
                    // Create a component.
                    var component = new List<T>();

                    // Performs a depth first search on the node, and adds the visited nodes to the component list.
                    undirectedGraph.DepthFirstSearchIterative(node, visited, (currentNode) => component.Add(currentNode));

                    // Add the component to the list.
                    components.Add(component);
                }
            }

            return components;
        }
        #endregion
    }
}
