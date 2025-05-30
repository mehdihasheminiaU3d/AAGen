using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AAGen.Shared;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AAGen
{
    [CreateAssetMenu(menuName = Constants.ContextMenus.OutputRulesMenu + nameof(CustomOutputRule))]
    public class CustomOutputRule : OutputRule
    {
        protected override bool DoesSubgraphMatchSelectionCriteria(SubgraphInfo subgraph)
        {
            return true;
        }

        protected override string CalculateName(SubgraphInfo subgraph)
        {
            if(subgraph.Nodes.Count > 1 && TryGetCommonParentFolderName(subgraph.Nodes.Select((x)=>x.AssetPath).ToList(), out var commonFolder))
            {
                return commonFolder + subgraph.HashOfSources.ToString();
            }

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
                string join = string.Empty;
                if (subgraph.Sources.Count < 4)
                {
                    join = string.Join("-", subgraph.Sources.Select((x) => x.FileName.RemoveExtension()));
                    join = join.Replace(" ", "_");
                }

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
        
        public static bool TryGetCommonParentFolderName(List<string> filePaths, out string folderName)
        {
            folderName = string.Empty;
            
            if (filePaths == null || filePaths.Count == 0)
                return false;

            var parentDirs = filePaths
                .Select(path => Path.GetDirectoryName(path)?.Replace('\\', '/').TrimEnd('/'))
                .Distinct()
                .ToList();

            if (parentDirs.Count != 1)
                return false;

            string commonDir = parentDirs[0];
            folderName = Path.GetFileName(commonDir);
            return true;
        }
    }
}
