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
            
            //report
            AddCommand(new ActionCommand(() => AddToReport($"Total Asset Count = {assetPaths.Length}")));
            
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

        void AddToReport(string value)
        {
            m_DataContainer.SummaryReport.TryAdd(value);
        }
    }
}