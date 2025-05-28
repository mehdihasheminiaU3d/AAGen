using System.Collections.Generic;
using AAGen.Shared;
using UnityEngine;

namespace AAGen
{
    [CreateAssetMenu(menuName = Constants.ContextMenus.OutputRulesMenu + nameof(DefaultRefinementRule))]
    public class DefaultRefinementRule : RefinementRule
    {
        public override void Execute(List<SubgraphInfo> subgraphs)
        {
        }
    }
}