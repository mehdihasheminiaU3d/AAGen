using System.Collections.Generic;
using System.Linq;
using AAGen.AssetDependencies;
using UnityEngine;

namespace AAGen
{
    public abstract class SubgraphSelector: ScriptableObject
    {
        protected Dictionary<int, SubgraphInfo> m_AllSubgraphs;
        protected DependencyGraph m_DependencyGraph;

        public virtual void Initialize(Dictionary<int, SubgraphInfo> allSubgraphs, DependencyGraph dependencyGraph)
        {
            m_AllSubgraphs = allSubgraphs;
            m_DependencyGraph = dependencyGraph;
        }

        public virtual List<SubgraphInfo> Select()
        {
            return m_AllSubgraphs.Values.Where(IsMatch).ToList();
        }
        
        protected abstract bool IsMatch(SubgraphInfo subgraph);
    }
}