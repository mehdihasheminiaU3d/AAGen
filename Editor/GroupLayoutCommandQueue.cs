using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.Settings;

namespace AAGen
{
    internal class GroupLayoutCommandQueue : CommandQueue
    {
        readonly DataContainer m_DataContainer;

        int m_SubgraphsProcessed;
        int m_GroupLayoutCreated;

        AddressableAssetGroupTemplate m_FallbackTemplate;
        
        public GroupLayoutCommandQueue(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;
            Title = nameof(GroupLayoutCommandQueue);
        }

        public override void PreExecute()
        {
            ClearQueue();
            m_DataContainer.GroupLayout = new Dictionary<string, GroupLayoutInfo>();
            
            //one subgraph maps to one group
            foreach (var pair in m_DataContainer.Subgraphs)
            {
                var hash = pair.Key;
                var subgraph = pair.Value;
                AddCommand(() => CreateGroupLayout(subgraph), hash.ToString());
            }
        }

        void CreateGroupLayout(SubgraphInfo subgraph)
        {
            m_SubgraphsProcessed++;

            var groupName = string.IsNullOrEmpty(subgraph.Name)
                ? OutputRule.GetFallbackName(subgraph) 
                : subgraph.Name;

            var templateName = string.IsNullOrEmpty(subgraph.AddressableTemplateName)
                ? OutputRule.GetFallbackTemplate().Name
                : subgraph.AddressableTemplateName;

            var groupLayoutInfo = new GroupLayoutInfo
            {
                TemplateName = templateName,
                Nodes = subgraph.Nodes.ToList()
            };

            if (groupLayoutInfo.Nodes.Count == 0)
                throw new Exception($"group node count == 0!"); //ToDo: Can this happen? we checked this in previous steps!

            m_DataContainer.GroupLayout.Add(groupName, groupLayoutInfo);
            m_GroupLayoutCreated++;
        }

        public override void PostExecute()
        {
            AppendToSummaryReport();
        }

        void AppendToSummaryReport()
        {
            if (!m_DataContainer.Settings.GenerateSummaryReport)
                return;

            var summary = $"\n=== Group Layout ===\n";
            summary += $"{nameof(m_SubgraphsProcessed).ToReadableFormat()} = {m_SubgraphsProcessed}\n";
            summary += $"{nameof(m_GroupLayoutCreated).ToReadableFormat()} = {m_GroupLayoutCreated}";
            
            m_DataContainer.SummaryReport.AppendLine(summary);
        }
    }
}