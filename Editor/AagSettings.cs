using System.Collections.Generic;
using UnityEngine;

namespace AAGen
{
    [CreateAssetMenu(menuName = Constants.ContextMenus.Root + "Settings")]
    internal class AagSettings : ScriptableObject
    {
        public List<InputFilterRule> _InputFilterRules;
        public List<MergeRule> _MergeRules;
        public List<GroupLayoutRule> _GroupLayoutRules;
    }
}
