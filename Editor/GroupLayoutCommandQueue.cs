using System;
using System.Collections.Generic;
using System.Linq;

namespace AAGen
{
    internal class GroupLayoutCommandQueue : CommandQueue
    {
        readonly DataContainer m_DataContainer;

        int m_SubgraphsProcessed;
        int m_GroupLayoutCreated;
        
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
                var templateName = m_DataContainer.Settings.m_DefaultGroupTemplate.Name;
                AddCommand(() => CreateGroupLayout(hash, subgraph, templateName), hash.ToString());
            }
        }

        void CreateGroupLayout(int hash, SubgraphInfo subgraph, string templateName)
        {
            m_SubgraphsProcessed++;
            
            var groupLayoutInfo = new GroupLayoutInfo()
            {
                TemplateName = templateName,
                Nodes = subgraph.Nodes.ToList()
            };
            
            if (groupLayoutInfo.Nodes.Count > 0)
            {
                var groupName = subgraph.Name;
                //What if the name is redundant?
                m_DataContainer.GroupLayout.Add(groupName, groupLayoutInfo);
                m_GroupLayoutCreated++;
            }
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