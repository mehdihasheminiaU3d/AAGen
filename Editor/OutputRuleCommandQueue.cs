using System.Collections.Generic;

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
            foreach (var outputRule in m_DataContainer.Settings.OutputRules)
            {
                var rule = outputRule;
                AddCommand(() => rule.Initialize(m_DataContainer));
                
                AddCommand(() => SelectSubgraphs(rule));
                AddCommand(() => rule.Modify(GetSelectedSubgraphs(rule)));
                AddCommand(() => rule.Rename(GetSelectedSubgraphs(rule)));
                AddCommand(() => rule.Refine(GetSelectedSubgraphs(rule)));
                AddCommand(() => rule.UnInit());
            }
        }
        
        public override void PostExecute()
        {
            //ToDo: Do we need to cleanup the protected fields of SOs?
        }
        
        void SelectSubgraphs(OutputRule rule)
        {
            m_SelectedSubgraphs.Add(rule.name, rule.Select());
        }

        List<SubgraphInfo> GetSelectedSubgraphs(OutputRule outputRule)
        {
            return m_SelectedSubgraphs[outputRule.name];
        }
    }
}