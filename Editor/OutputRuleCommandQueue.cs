using System;
using System.Collections.Generic;
using System.Linq;
using AAGen.AssetDependencies;
using UnityEngine;

namespace AAGen
{
    internal class OutputRuleCommandQueue : CommandQueue
    {
        readonly DataContainer m_DataContainer;
        
        Dictionary<string, List<SubgraphInfo>> m_SelectedSubgraphs;
        
        public OutputRuleCommandQueue(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;
            Title = nameof(OutputRuleCommandQueue);
        }

        public override void PreExecute()
        {
            ClearQueue();
            m_SelectedSubgraphs = new Dictionary<string, List<SubgraphInfo>>();
            foreach (var outputRuleSet in m_DataContainer.Settings.OutputRules)
            {
                var ruleSet = outputRuleSet;
                AddCommand(() => CategorizeSebgraphs(ruleSet));
                AddCommand(() => ruleSet.RefinementRule.Execute(GetSelectedSubgraphs(ruleSet)));
                AddCommand(() => ruleSet.NamingRule.AssignNames(GetSelectedSubgraphs(ruleSet)));
            }
        }
        
        public override void PostExecute()
        {
            //ToDo: Do we need to cleanup the protected fields of SOs?
        }
        
        void CategorizeSebgraphs(OutputRule outputRule)
        {
            outputRule.SubgraphSelector.Initialize(m_DataContainer.Subgraphs, m_DataContainer.DependencyGraph);
            m_SelectedSubgraphs.Add(outputRule.Name, outputRule.SubgraphSelector.Select());
        }

        List<SubgraphInfo> GetSelectedSubgraphs(OutputRule outputRule)
        {
            return m_SelectedSubgraphs[outputRule.Name];
        }
    }
}