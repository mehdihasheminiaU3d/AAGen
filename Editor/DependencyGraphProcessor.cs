using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace AAGen
{
    /// <summary>
    /// Serves as a base class for graph processors. Graph processors encapsulate functionality to analyze and extract specific
    /// data from the dependency graph. They are designed to separate UI concerns from processing algorithms.
    /// </summary>
    internal abstract class DependencyGraphProcessor
    {
        protected readonly DependencyGraph _dependencyGraph;
        protected readonly EditorUiGroup _uiGroup;
        
        private double _iterationStartTime;

        protected DependencyGraphProcessor(DependencyGraph dependencyGraph, EditorUiGroup uiGroup)
        {
            _dependencyGraph = dependencyGraph;
            _uiGroup = uiGroup;
        }

        protected virtual void Init()
        {
            _iterationStartTime = EditorApplication.timeSinceStartup;
        }

        /// <summary>
        /// Finds the ending node in the hierarchy of the input asset.
        /// If a normal graph is provided, the output identifies the sink nodes of the hierarchy.
        /// If a transposed graph is provided, the output identifies the source nodes of the hierarchy.
        /// </summary>
        /// <param name="graph">The graph to analyze.</param>
        /// <param name="node">The starting node for the search.</param>
        /// <returns>The identified ending node, either a sink or a source, depending on the graph type.</returns>
        protected static HashSet<AssetNode> FindLeafNodes(DependencyGraph graph, AssetNode node)
        {
            var allPaths = graph.DepthFirstSearchForAllPaths(node, IsSource);
            
            var result = allPaths.Select(path => path[^1]).ToHashSet();
            return result;
            
            bool IsSource(AssetNode endNode)
            {
                return graph.GetNeighbors(endNode).Count == 0;
            }
        }

        protected static string PrintPaths(List<List<AssetNode>> paths)
        {
            string str = null;

            for (var i = 0; i < paths.Count; i++)
            {
                if (paths.Count > 1)
                    str += $"Path{i + 1}: ";
                
                var path = paths[i];
                if (path.Count > 0)
                    str += $"{string.Join(" -> ", path.Select(node => node.FileName))} \n";
            }

            return str;
        }

        protected bool ShouldUpdateUi
        {
            get
            {
                if (EditorApplication.timeSinceStartup - _iterationStartTime > ProjectSettingsProvider.TargetEditorFrameTime)
                {
                    _iterationStartTime = EditorApplication.timeSinceStartup;
                    return true;
                }

                return false;
            }
        }
    }
}
