using System;
using AAGen.AssetDependencies;
using UnityEditor;

namespace AAGen
{
    internal class DependencyGraphGeneratorProcessor : NodeProcessor
    {
        public DependencyGraphGeneratorProcessor(DataContainer dataContainer)
        {
            dataContainer.m_DependencyGraph = new DependencyGraph();
            
            var root = new ProcessingUnit(null) { Name = "Root" };
            
            var assetPaths = AssetDatabase.GetAllAssetPaths();
            foreach (var assetPath in assetPaths)
            {
                root.AddChild(new AssetProcessingUnit(dataContainer.m_DependencyGraph, assetPath));
            }
            
            SetRoot(root); 
        }
    }
    
    public class AssetProcessingUnit : ProcessingNode
    {
        readonly DependencyGraph m_DependencyGraph;
        readonly string m_AssetPath;

        public AssetProcessingUnit(DependencyGraph dependencyGraph, string assetPath)
        {
            m_DependencyGraph = dependencyGraph;
            m_AssetPath = assetPath;
        }

        protected override void OnProcess()
        {
            DependencyGraphGeneratorCore.AddAssetToGraph(m_DependencyGraph, m_AssetPath);
        }
    }
}