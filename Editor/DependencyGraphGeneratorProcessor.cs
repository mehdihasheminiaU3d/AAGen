using System;
using AAGen.AssetDependencies;
using UnityEditor;

namespace AAGen
{
    internal class DependencyGraphGeneratorProcessor : NodeProcessor
    {
        DataContainer m_DataContainer;
        
        public DependencyGraphGeneratorProcessor(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;
            m_DataContainer.m_DependencyGraph = new DependencyGraph();
            
            var root = new ProcessingUnit(null) { Name = "Root" };
            
            var assetPaths = AssetDatabase.GetAllAssetPaths();
            foreach (var assetPath in assetPaths)
            {
                root.AddChild(new ProcessingUnit(() => AddAssetToDependencyGraph(assetPath)));
            }
            
            SetRoot(root); 
        }

        void AddAssetToDependencyGraph(string assetPath)
        {
            DependencyGraphGeneratorCore.AddAssetToGraph(m_DataContainer.m_DependencyGraph, assetPath);
        }
    }
}