using AAGen.AssetDependencies;
using UnityEditor;

namespace AAGen
{
    internal class DependencyGraphCommandQueue : NewCommandQueue
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
            ClearQueue();
            
            m_DataContainer.DependencyGraph = new DependencyGraph();
            
            var assetPaths = AssetDatabase.GetAllAssetPaths();
            m_TotalAssetCount = assetPaths.Length;
            
            foreach (var assetPath in assetPaths)
            {
                var path = assetPath; // avoid closure capturing loop variable
                AddCommand(new NewActionCommand()
                {
                    Action = () => AddAssetToDependencyGraph(path),
                    Info = path,
                });
            }
        }

        void AddAssetToDependencyGraph(string assetPath)
        {
            DependencyGraphGeneratorCore.AddAssetToGraph(m_DataContainer.DependencyGraph, assetPath);
        }

        public override void PostExecute()
        {
            AppendToSummaryReport();
        }

        void AppendToSummaryReport()
        {
            if (!m_DataContainer.Settings.GenerateSummaryReport)
                return;

            var summary = $"\n=== Dependency Graph ===\n";
            summary += $"{nameof(m_TotalAssetCount).ToReadableFormat()} = {m_TotalAssetCount}\n";
            summary += $"Total Node Count = {m_DataContainer.DependencyGraph.NodeCount}";
            
            m_DataContainer.SummaryReport.AppendLine(summary); 
        }
    }
}
