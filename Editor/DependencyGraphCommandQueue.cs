using AAGen.AssetDependencies;
using UnityEditor;

namespace AAGen
{
    internal class DependencyGraphCommandQueue : CommandQueue
    {
        readonly DataContainer m_DataContainer;
        
        public DependencyGraphCommandQueue(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;
            m_DataContainer.m_DependencyGraph = new DependencyGraph();
            Title = nameof(DependencyGraphCommandQueue);
        }
        
        public override void PreExecute()
        {
            var assetPaths = AssetDatabase.GetAllAssetPaths();
            foreach (var assetPath in assetPaths)
            {
                AddCommand(new ActionCommand(() => AddAssetToDependencyGraph(assetPath), assetPath));
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