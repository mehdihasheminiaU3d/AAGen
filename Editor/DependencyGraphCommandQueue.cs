using AAGen.AssetDependencies;
using UnityEditor;

namespace AAGen
{
    internal class DependencyGraphCommandQueue : CommandQueue
    {
        readonly DataContainer m_DataContainer;
        
        //Summary report values
        int m_TotalAssetCount = 0;
        
        public DependencyGraphCommandQueue(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;
            Title = nameof(DependencyGraphCommandQueue);
        }
        
        public override void PreExecute()
        {
            m_DataContainer.DependencyGraph = new DependencyGraph();
            
            var assetPaths = AssetDatabase.GetAllAssetPaths();
            m_TotalAssetCount = assetPaths.Length;
            
            foreach (var assetPath in assetPaths)
            {
                AddCommand(new ActionCommand(() => AddAssetToDependencyGraph(assetPath), assetPath));
            }
            AddCommand(new ActionCommand(CalculateTransposedGraph));
            
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

        public override void PostExecute()
        {
            AppendToSummaryReport();
        }

        void AppendToSummaryReport()
        {
            if (!m_DataContainer.Settings.GenerateSummaryReport)
                return;

            var summary =
                $"Total Asset Count = {m_TotalAssetCount} \n" +
                $"Total Node Count = {m_DataContainer.DependencyGraph.NodeCount}";
            
            m_DataContainer.SummaryReport.AppendLine(summary); 
        }
    }
}