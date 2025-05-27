using System.Collections.Generic;
using System.Linq;
using AAGen.AssetDependencies;
using UnityEditor;

namespace AAGen
{
    internal class InputFilterCommandQueue : NewCommandQueue 
    {
        readonly DataContainer m_DataContainer;
        
        //Summary report values
        int m_IgnoredUnsupportedFiles = 0;
        int m_NodesIgnoredByRules = 0;
        int m_IgnoredScenesInBuildProfile = 0;
        
        public InputFilterCommandQueue(DataContainer dataContainer) 
        {
            m_DataContainer = dataContainer;
            Title = nameof(InputFilterCommandQueue);
        }

        public override void PreExecute()
        {
            ClearQueue();
            m_DataContainer.IgnoredAssets = new HashSet<AssetNode>();
            
            foreach (var node in m_DataContainer.DependencyGraph.GetAllNodes())
            {
                var localNode = node; // avoid closure capturing loop variable
                var fileName = node.FileName; 
                
                AddCommand(() => AddRuledFileToIgnoredList(localNode), fileName);
                AddCommand(() => AddUnsupportedFileToIgnoreAssets(localNode), fileName);
            }
            
            AddCommand(AddBuiltinScenesToIgnoredList, nameof(AddBuiltinScenesToIgnoredList));
        }

        void AddRuledFileToIgnoredList(AssetNode node)
        {
            foreach (var inputFilterRule in m_DataContainer.Settings.InputFilterRules)
            {
                var isSource = m_DataContainer.DependencyGraph.IsSourceNode(node);
                if (inputFilterRule.ShouldIgnoreNode(node, isSource))
                {
                    m_DataContainer.IgnoredAssets.Add(node);
                    m_NodesIgnoredByRules++;
                }
            }
        }
        
        void AddUnsupportedFileToIgnoreAssets(AssetNode node)
        {
            var mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(node.AssetPath);
            if (mainAssetType == null || mainAssetType == typeof(DefaultAsset))
            {
                m_DataContainer.IgnoredAssets.Add(node);
                m_IgnoredUnsupportedFiles++;
            }
        }
        
        void AddBuiltinScenesToIgnoredList()
        {
            var scenes = EditorBuildSettings.scenes; //scenes in build profile
            m_IgnoredScenesInBuildProfile = scenes.Length;
            if (scenes.Length == 0)
                return;
            
            var sceneNodes = scenes.Select(scene => AssetNode.FromAssetPath(scene.path)).ToHashSet();
            m_DataContainer.IgnoredAssets.UnionWith(sceneNodes);
        }
        
        public override void PostExecute()
        {
            AppendToSummaryReport();
        }

        void AppendToSummaryReport()
        {
            if (!m_DataContainer.Settings.GenerateSummaryReport)
                return;

            var summary = $"\n=== Input Filter ===\n";
            summary += $"{nameof(m_NodesIgnoredByRules).ToReadableFormat()} = {m_NodesIgnoredByRules} \n";
            summary += $"{nameof(m_IgnoredUnsupportedFiles).ToReadableFormat()} = {m_IgnoredUnsupportedFiles} \n";
            summary += $"{nameof(m_IgnoredScenesInBuildProfile).ToReadableFormat()} = {m_IgnoredScenesInBuildProfile}\n";
            summary += $"----------\n";
            var nodesPassed = m_DataContainer.DependencyGraph.NodeCount -
                              (m_NodesIgnoredByRules + m_IgnoredUnsupportedFiles + m_IgnoredScenesInBuildProfile);
            summary += $"{nameof(nodesPassed).ToReadableFormat()} = {nodesPassed}";
            
            m_DataContainer.SummaryReport.AppendLine(summary);
        }
    }
}