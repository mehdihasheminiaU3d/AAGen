using System;
using AAGen.AssetDependencies;
using UnityEditor;

namespace AAGen
{
    internal class DependencyGraphGeneratorQueue : CommandQueue
    {
        DataContainer m_DataContainer;
        
        public DependencyGraphGeneratorQueue(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;
            m_DataContainer.m_DependencyGraph = new DependencyGraph();
            
            
            var assetPaths = AssetDatabase.GetAllAssetPaths();
            foreach (var assetPath in assetPaths)
            {
                AddCommand(new ActionCommand(() => AddAssetToDependencyGraph(assetPath)));
            }
            AddCommand(new ActionCommand(CalculateTransposedGraph));
            
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