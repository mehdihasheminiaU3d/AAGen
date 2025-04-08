using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AAGen.AssetDependencies;
using AAGen.Shared;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace AAGen
{
    internal class GroupLayoutNodeProcessor : NodeProcessor
    {
        public GroupLayoutNodeProcessor(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;
            
            m_DataContainer._groupLayout = new Dictionary<string, GroupLayoutInfo>();
            
            var root = new ProcessingUnit(null) { Name = "root" };
            
            //one subgraph maps to one group
            foreach (var pair in m_DataContainer._allSubgraphs)
            {
                var hash = pair.Key;
                var subgraph = pair.Value;
                var templateName = m_DataContainer.Settings._GroupLayoutRules[0].TemplateName; //<--------only one for now
                root.AddChild(new ProcessingUnit(() => CreateGroupLayout(hash, subgraph, templateName)));
            }
            
            SetRoot(root);
        }
        
        DataContainer m_DataContainer;

        void CreateGroupLayout(int hash, SubgraphInfo subgraph, string templateName)
        {
            var sources = m_DataContainer._subgraphSources[hash];

            var groupName = GetSubgraphName(subgraph, sources); //ToDo: Add a naming settings to customize names
            if (m_DataContainer._groupLayout.ContainsKey(groupName))
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
                m_DataContainer._groupLayout.Add(groupName, groupLayoutInfo);
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
    }
}