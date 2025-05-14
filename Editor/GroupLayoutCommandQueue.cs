using System;
using System.Collections.Generic;
using System.Linq;
using AAGen.AssetDependencies;
using UnityEngine;

namespace AAGen
{
    internal class GroupLayoutCommandQueue : CommandQueue
    {
        readonly DataContainer m_DataContainer;
        
        public GroupLayoutCommandQueue(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;
            Title = nameof(GroupLayoutCommandQueue);
        }

        public override void PreExecute()
        {
            m_DataContainer.GroupLayout = new Dictionary<string, GroupLayoutInfo>();
            
            //one subgraph maps to one group
            foreach (var pair in m_DataContainer.Subgraphs)
            {
                var hash = pair.Key;
                var subgraph = pair.Value;
                var templateName = m_DataContainer.Settings.m_DefaultGroupTemplate.Name;
                AddCommand(new ActionCommand(() => CreateGroupLayout(hash, subgraph, templateName), hash.ToString()));
            }
            
            EnqueueCommands();
        }

        void CreateGroupLayout(int hash, SubgraphInfo subgraph, string templateName)
        {
            var sources = m_DataContainer.SubgraphSources[hash];

            var groupName = GetSubgraphName(subgraph, sources); //ToDo: Add a naming settings to customize names
            if (m_DataContainer.GroupLayout.ContainsKey(groupName))
            {
                //If name already registered, switch to fallback name
                groupName += $"_{hash}";
            }

            var groupLayoutInfo = new GroupLayoutInfo()
            {
                TemplateName = templateName,
                Nodes = subgraph.Nodes.ToList()
            };
                    
            if (groupLayoutInfo.Nodes.Count > 0)
                m_DataContainer.GroupLayout.Add(groupName, groupLayoutInfo);
        }
        
        static string GetSubgraphName(SubgraphInfo subgraph, HashSet<AssetNode> sources)
        {
            string name = null;
            if (subgraph.IsShared) 
            {
                name = $"Shared_";
                if (subgraph.Nodes.Count == 1)
                {
                    var node = subgraph.Nodes.ToList()[0];
                    name += node.FileName;
                }
                else
                {
                    name += SubgraphProcessor.CalculateHashForSources(sources).ToString();
                }
            }
            else
            {
                if(sources is { Count: > 0 })
                {
                    var sourceNode = sources.ToList()[0];
                    var n = sourceNode.FileName;
                    name = $"{n}_Assets";
                }
            }
            
            if (string.IsNullOrEmpty(name)) // ToDo: Is this condition ever met?
            {
                name = $"NullName_{Guid.NewGuid().ToString()}"; 
                Debug.LogError($"empty subgraph name");
            }
                
            return name;
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
                $"";
            
            m_DataContainer.SummaryReport.AppendLine(summary);
        }
    }
}