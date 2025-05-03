using System;
using System.Collections.Generic;
using AAGen.AssetDependencies;
using UnityEngine;
using UnityEngine.Serialization;

namespace AAGen
{
    public enum PathMatchCondition
    {
        Contains,
        DoesNotContain
    }
    
    public enum InclusionAction
    {
        IgnoreIfSourceOnly, // Ignores only if not used by other assets, i.e. it's a source node
        AlwaysIgnore,       // Potential Risk: May prevent addition of required shared nodes to addressable assets 
        AlwaysInclude       //Don't Ignore or re-add if ignored
    }

    [Serializable]
    public struct PathFilterCriterion
    {
        public string m_Description;
        public PathMatchCondition m_PathMatchCondition;
        public string m_PathKeyword;
        public InclusionAction m_InclusionAction;
    }
    
    [CreateAssetMenu(menuName = "AAGen/Settings/" + nameof(PathFilterRule))]
    internal class PathFilterRule : InputFilterRule
    {
        [SerializeField, Tooltip("Conditions for including or ignoring assets based on their file paths.")]
        public List<PathFilterCriterion> m_Criteria;
        
        public override bool ShouldIgnoreNode(AssetNode node, bool isSourceNode) 
        {
            foreach (var inclusionCriterion in m_Criteria)
            {
                if(string.IsNullOrEmpty(inclusionCriterion.m_PathKeyword))
                    continue;
                
                var nodePathContainsKeyword = NodePathContains(node, inclusionCriterion.m_PathKeyword);
                
                var criterionAppliesToNode =
                    (inclusionCriterion.m_PathMatchCondition == PathMatchCondition.Contains && nodePathContainsKeyword) ||
                    (inclusionCriterion.m_PathMatchCondition == PathMatchCondition.DoesNotContain && !nodePathContainsKeyword);

                if (criterionAppliesToNode)
                    return ShouldIgnoreBasedOnOptions(inclusionCriterion.m_InclusionAction, isSourceNode);
            }
            
            return false;
        }
        
        bool NodePathContains(AssetNode node, string value)
        {
            return node.AssetPath.Contains(value, StringComparison.OrdinalIgnoreCase);
        }

        static bool ShouldIgnoreBasedOnOptions(InclusionAction inclusionAction, bool isSourceNode)
        {
            switch (inclusionAction)
            {
                case InclusionAction.AlwaysIgnore:
                case InclusionAction.IgnoreIfSourceOnly when isSourceNode:
                    return true;
                case InclusionAction.AlwaysInclude:
                    return false;
                default:
                    return false;
            }
        }
    }
}
