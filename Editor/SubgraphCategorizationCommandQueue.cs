using System.Linq;
using UnityEngine;

namespace AAGen
{
    internal class SubgraphCategorizationCommandQueue : CommandQueue
    {
        readonly DataContainer m_DataContainer;
        SubgraphCategoryID m_DefaultSubgraphCategoryID;
        
        public SubgraphCategorizationCommandQueue(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;
            Title = nameof(SubgraphCommandQueue);
        }

        public override void PreExecute()
        {
            m_DefaultSubgraphCategoryID = ScriptableObject.CreateInstance<UncategorizedSubgraphID>();
            m_DefaultSubgraphCategoryID.name = $"Uncategorized";
            m_DataContainer.Settings.DefaultCategoryID = m_DefaultSubgraphCategoryID; //ToDo: Could be done before
            
            foreach (var pair in m_DataContainer.Subgraphs)
            {
                var subgraph = pair.Value;
                AddCommand(new ActionCommand(() => CategorizeSubgraph(subgraph)));
            }
            
            EnqueueCommands();
        }

        void CategorizeSubgraph(SubgraphInfo subgraph)
        {
            var subgraphCategoryIDs = m_DataContainer.Settings.SubgraphCategoryIds;
            var matchingCategoryID = m_DefaultSubgraphCategoryID;
            
            //Finds first matching category (prioritizes smaller indices)
            foreach (var categoryID in subgraphCategoryIDs)
            {
                if (categoryID.MatchesCategoryRule(subgraph))
                {
                    matchingCategoryID = categoryID;
                    break;
                }
            }

            subgraph.CategoryID = matchingCategoryID;
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
            
            var allSubgraphs = m_DataContainer.Subgraphs.Values.ToList();
            
            foreach (var categoryId in m_DataContainer.Settings.SubgraphCategoryIds)
            {
                var subgraphsInCategory = SubgraphInfo.SelectSubgraphsByCategory(allSubgraphs, categoryId);
                summary += $"{categoryId.name} = {subgraphsInCategory.Count} \n";
            }
            
            m_DataContainer.SummaryReport.AppendLine(summary);
        }
    }
}