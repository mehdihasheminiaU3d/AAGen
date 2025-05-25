using AAGen.AssetDependencies;
using AAGen.Shared;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
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
                AddCommand(() => AddAssetToDependencyGraph(path), path);
            }

            if (m_DataContainer.Settings.SaveGraphOnDisk)
            {
                AddCommand(SaveGraphOnDisk, "Saving DependencyGraph");
            }
        }

        void AddAssetToDependencyGraph(string assetPath)
        {
            DependencyGraphGeneratorCore.AddAssetToGraph(m_DataContainer.DependencyGraph, assetPath);
        }

        void SaveGraphOnDisk()
        {
            var data = JsonConvert.SerializeObject(m_DataContainer.DependencyGraph, Formatting.Indented);
            FileUtils.SaveToFile(Constants.DependencyGraphFilePath,data);
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
