using System;
using AAGen.AssetDependencies;
using UnityEditor;

namespace AAGen
{
    internal class DependencyGraphGeneratorProcessor : CommandProcessor
    {
        DataContainer m_DataContainer;
        
        public DependencyGraphGeneratorProcessor(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;
            m_DataContainer.m_DependencyGraph = new DependencyGraph();
            
            
            var assetPaths = AssetDatabase.GetAllAssetPaths();
            foreach (var assetPath in assetPaths)
            {
                AddCommand(new ProcessingUnit(() => AddAssetToDependencyGraph(assetPath)));
            }
            AddCommand(new ProcessingUnit(CalculateTransposedGraph));
            
            EnqueueCommands(); 
        }

        void AddAssetToDependencyGraph(string assetPath)
        {
            DependencyGraphGeneratorCore.AddAssetToGraph(m_DataContainer.m_DependencyGraph, assetPath);
        }

        void CalculateTransposedGraph()
        {
            m_DataContainer.m_TransposedGraph = new DependencyGraph(m_DataContainer.m_DependencyGraph.GetTransposedGraph());
        }
    }
}