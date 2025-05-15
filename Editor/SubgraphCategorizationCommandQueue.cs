using System;
using System.Collections.Generic;
using UnityEngine;

namespace AAGen
{
    internal class SubgraphCategorizationCommandQueue : CommandQueue
    {
        readonly DataContainer m_DataContainer;
        readonly SubgraphCategoryID m_DefaultSubgraphID;
        
        public SubgraphCategorizationCommandQueue(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;
            Title = nameof(SubgraphCommandQueue);
            m_DefaultSubgraphID = ScriptableObject.CreateInstance<UncategorizedSubgraphID>();
            m_DefaultSubgraphID.name = $"Uncategorized";
        }

        public override void PreExecute()
        {
            m_DataContainer.SubgraphCategories = new Dictionary<SubgraphCategoryID, List<SubgraphInfo>>();

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
            var matchingCategoryID = m_DefaultSubgraphID;
            
            //Finds first matching category (prioritizes smaller indices)
            foreach (var categoryID in subgraphCategoryIDs)
            {
                if (categoryID.MatchesCategoryRule(subgraph))
                {
                    matchingCategoryID = categoryID;
                    break;
                }
            }

            var subgraphCategories = m_DataContainer.SubgraphCategories;
            if (!subgraphCategories.ContainsKey(matchingCategoryID))
            {
                subgraphCategories[matchingCategoryID] = new List<SubgraphInfo>();
            }
            subgraphCategories[matchingCategoryID].Add(subgraph);
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
            foreach (var category in m_DataContainer.SubgraphCategories)
            {
                var categoryID = category.Key;
                var subgraphsInCategory = category.Value;
                summary += $"{categoryID.name} = {subgraphsInCategory.Count} \n";
            }
            
            m_DataContainer.SummaryReport.AppendLine(summary);
        }
    }
}