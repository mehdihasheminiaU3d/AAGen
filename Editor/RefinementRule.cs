using System.Collections.Generic;
using AAGen.AssetDependencies;
using UnityEngine;

namespace AAGen
{
    public abstract class RefinementRule : ScriptableObject
    {
        public abstract void Execute(List<SubgraphInfo> subgraphs);

        protected DependencyGraph m_DependencyGraph;

        public void Initialize(DependencyGraph dependencyGraph)
        {
            m_DependencyGraph = dependencyGraph;
        }
    }
}