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
            Title = nameof(DependencyGraphCommandQueue);
        }
        
        public override void PreExecute()
        {
            m_DataContainer.DependencyGraph = new DependencyGraph();
            
            var assetPaths = AssetDatabase.GetAllAssetPaths();
            
            foreach (var assetPath in assetPaths)
            {
                AddCommand(new ActionCommand(() => AddAssetToDependencyGraph(assetPath), assetPath));
            }
            AddCommand(new ActionCommand(CalculateTransposedGraph));
            
            AddCommand(new ActionCommand(() => m_DataContainer.SummaryReport.TryAdd($"Total Asset Count = {assetPaths.Length}")));
            AddCommand(new ActionCommand(() => m_DataContainer.SummaryReport.TryAdd($"Total Node Count = {m_DataContainer.DependencyGraph.NodeCount}")));
            
            EnqueueCommands(); 
        }

        void AddAssetToDependencyGraph(string assetPath)
        {
            DependencyGraphGeneratorCore.AddAssetToGraph(m_DataContainer.DependencyGraph, assetPath);
        }

        void CalculateTransposedGraph()
        {
            m_DataContainer.TransposedGraph = new DependencyGraph(m_DataContainer.DependencyGraph.GetTransposedGraph());
        }
    }
}