using AAGen.Shared;
using UnityEditor;

namespace AAGen.AssetDependencies
{
    internal class DependencyGraphGeneratorCore
    {
        private void CreateGraph()
        {
            var assetPaths = AssetDatabase.GetAllAssetPaths();
            DependencyGraph dependencyGraph = new DependencyGraph();

            for (int i = 0; i < assetPaths.Length; i++)
            {
                string assetPath = assetPaths[i]; 
                AddAssetToGraph(dependencyGraph, assetPath);
            }
        }
        
        public static void AddAssetToGraph(DependencyGraph dependencyGraph, string assetPath)
        {
            if (FileUtils.ShouldIgnoreAsset(assetPath))
                return;

            var dependencies = AssetDatabase.GetDependencies(assetPath, false);

            if (dependencies == null || dependencies.Length == 0)
            {
                dependencyGraph.AddNode(assetPath);
                return;
            }

            foreach (var dependency in dependencies)
            {
                if (FileUtils.ShouldIgnoreAsset(dependency))
                    continue;

                dependencyGraph.AddEdge(assetPath, dependency);
            }
        }
    }
}