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

        int m_SubgraphsProcessed;
        int m_GroupLayoutCreated;
        
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
            m_SubgraphsProcessed++;
            
            var groupLayoutInfo = new GroupLayoutInfo()
            {
                TemplateName = templateName,
                Nodes = subgraph.Nodes.ToList()
            };
            
            if (groupLayoutInfo.Nodes.Count > 0)
            {
                var groupName = CalculateGroupName(hash, subgraph);
                //What if the name is redundant?
                m_DataContainer.GroupLayout.Add(groupName, groupLayoutInfo);
                m_GroupLayoutCreated++;
            }
        }

        string CalculateGroupName(int hash, SubgraphInfo subgraph)
        {
            return FindNamingRuleForSubgraph(subgraph).CalculateGroupName(hash, subgraph);
        }
        
        static string GetSubgraphName_Old(SubgraphInfo subgraph, HashSet<AssetNode> sources)
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

            var summary = $"\n=== Group Layout ===\n";
            summary += $"{nameof(m_SubgraphsProcessed).ToReadableFormat()} = {m_SubgraphsProcessed}\n";
            summary += $"{nameof(m_GroupLayoutCreated).ToReadableFormat()} = {m_GroupLayoutCreated}";
            
            m_DataContainer.SummaryReport.AppendLine(summary);
        }
        
        /// <summary>
        /// Returns the first naming rule found for that matches the subgraph category
        /// If nothing is found, it returns the default naming rule
        /// </summary>
        /// <param name="subgraph"></param>
        /// <returns></returns>
        AddressableGroupNamingRule FindNamingRuleForSubgraph(SubgraphInfo subgraph)
        {
            var settings = m_DataContainer.Settings;
            var matchingNamingRule = settings.DefaultNamingRule;
            foreach (var namingRule in settings.NamingRules)
            {
                if (namingRule.m_CategoryID == subgraph.CategoryID)
                {
                    matchingNamingRule = namingRule;
                    break;
                }
            }
            return matchingNamingRule;
        }
    }
}