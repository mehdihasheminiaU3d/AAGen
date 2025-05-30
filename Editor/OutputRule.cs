using System.Collections.Generic;
using System.Linq;
using AAGen.AssetDependencies;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace AAGen
{
    public abstract class OutputRule : ScriptableObject
    {
        [SerializeField]
        protected AddressableAssetGroupTemplate m_Template;
        
        DataContainer m_DataContainer;
        protected DependencyGraph m_DependencyGraph => m_DataContainer.DependencyGraph;
        
        public virtual void Initialize(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;
        }
        
        public virtual List<SubgraphInfo> Select()
        {
            return m_DataContainer.Subgraphs.Values.Where(DoesSubgraphMatchSelectionCriteria).ToList();
        }
        
        public virtual void Modify(List<SubgraphInfo> subgraphs)
        {
        }
        
        public virtual void Rename(List<SubgraphInfo> subgraphs)
        {
            foreach (var subgraph in subgraphs)
            {
                subgraph.Name = CalculateName(subgraph);
            }
        }
        
        public virtual void Refine(List<SubgraphInfo> subgraphs)
        {
            var template = m_Template != null ? m_Template : GetFallbackTemplate();
            
            foreach (var subgraph in subgraphs)
            {
                subgraph.AddressableTemplateName = template.name;
            }
        }

        public virtual void UnInit()
        {
            m_DataContainer = null;
        }
        
        protected abstract bool DoesSubgraphMatchSelectionCriteria(SubgraphInfo subgraph);
        protected abstract string CalculateName(SubgraphInfo subgraph);

        public static string GetFallbackName(SubgraphInfo subgraph)
        {
            return subgraph.HashOfSources.ToString();
        }

        public static AddressableAssetGroupTemplate GetFallbackTemplate()
        {
            return AddressableUtil.FindDefaultAddressableGroupTemplate();
        }
    }
}