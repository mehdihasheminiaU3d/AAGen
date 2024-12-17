using System.Collections.Generic;
using UnityEngine;

namespace AAGen.Editor.DependencyGraph
{
    [CreateAssetMenu(menuName = "Dependency Graph/Automated Asset Grouping/Settings")]
    internal class AagSettings : ScriptableObject
    {
        public List<InputFilterRule> _InputFilterRules;
        public List<MergeRule> _MergeRules;
        public List<GroupLayoutRule> _GroupLayoutRules;
    }
}
