using System.Linq;
using UnityEngine;

namespace AAGen
{
    internal class SubgraphCategorizationCommandQueue : CommandQueue
    {
        readonly DataContainer m_DataContainer;
        
        public SubgraphCategorizationCommandQueue(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;
            Title = nameof(SubgraphCategorizationCommandQueue);
        }

        public override void PreExecute()
        {
            ClearQueue();
            foreach (var pair in m_DataContainer.Subgraphs)
            {
                var subgraph = pair.Value;
                AddCommand(() => CategorizeSubgraph(subgraph));
            }
        }

        void CategorizeSubgraph(SubgraphInfo subgraph)
        {
            //Finds first matching category 
            foreach (var categoryID in m_DataContainer.Settings.SubgraphCategoryIds)
            {
                if (categoryID.DoesSubgraphMatchCategory(subgraph, m_DataContainer))
                {
                    subgraph.CategoryID = categoryID;
                    break;
                }
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
            
            var summary = $"\n=== Subgraph Categories ===\n";
            
            foreach (var kvp in m_DataContainer.GetSubgraphsGroupedByCategory())
            {
                var category = kvp.Key;
                var subgraphsInCategory = kvp.Value;
                summary += $"{category.name} = {subgraphsInCategory.Count} \n";
            }
            
            m_DataContainer.SummaryReport.AppendLine(summary);
        }
    }
}