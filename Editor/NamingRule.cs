using System.Collections.Generic;
using UnityEngine;

namespace AAGen
{
    public abstract class NamingRule : ScriptableObject
    {
        public virtual void AssignNames(List<SubgraphInfo> subgraphs)
        {
            foreach (var subgraph in subgraphs)
            {
                subgraph.Name = CalculateName(subgraph);
            }
        }

        protected abstract string CalculateName(SubgraphInfo subgraph);
    }
}