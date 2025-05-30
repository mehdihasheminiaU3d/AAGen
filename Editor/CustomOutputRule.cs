using System.Linq;
using AAGen.Shared;
using UnityEngine;

namespace AAGen
{
    [CreateAssetMenu(menuName = Constants.ContextMenus.OutputRulesMenu + nameof(CustomOutputRule))]
    public class CustomOutputRule : OutputRule
    {
        protected override bool DoesSubgraphMatchSelectionCriteria(SubgraphInfo subgraph)
        {
            return subgraph.Nodes.Count > 1;
        }

        protected override string CalculateName(SubgraphInfo subgraph)
        {
            if (SubgraphTopologyUtil.IsSingleSourceNode(subgraph, m_DependencyGraph))
            {
                if (subgraph.Sources.Count == 1)
                {
                    var source = subgraph.Sources.ToList()[0];
                    return $"Single source {source.FileName.RemoveExtension()} R{RandInt}";
                }
            }

            if (SubgraphTopologyUtil.IsHierarchy(subgraph, m_DependencyGraph))
            {
                if (subgraph.Sources.Count == 1)
                {
                    var source = subgraph.Sources.ToList()[0];
                    return $"Hierarchy of a single source {source.FileName.RemoveExtension()} R{RandInt}";
                }
            }

            if (SubgraphTopologyUtil.IsSingleSinkNode(subgraph, m_DependencyGraph))
            {
                if (subgraph.Nodes.Count == 1)
                {
                    var node = subgraph.Nodes.ToList()[0];
                    return $"Single sink {node.FileName.RemoveExtension()} R{RandInt}";
                }
            }

            if (SubgraphTopologyUtil.IsShared(subgraph))
            {
                var join = string.Join("-", subgraph.Sources.Select((x) => x.FileName.RemoveExtension()));
                join = join.Replace(" ", "_");
                return $"Shared by {subgraph.Sources.Count} {join} {subgraph.HashOfSources.ToString()} R{RandInt}";
            }

            if (SubgraphTopologyUtil.IsSingleIsolatedNode(subgraph, m_DependencyGraph))
            {
                if (subgraph.Nodes.Count == 1)
                {
                    var node = subgraph.Nodes.ToList()[0];
                    return $"Isolated {node.FileName.RemoveExtension()} R{RandInt}";
                }
            }

            return subgraph.HashOfSources.ToString();
        }

        int RandInt
        {
            get
            {
                // return 1;
                return Random.Range(1, 100);
            }
        }
    }
}
