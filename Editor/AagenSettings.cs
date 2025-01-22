using System.Collections.Generic;
using UnityEngine;
using AAGen.Shared;

namespace AAGen
{
    [CreateAssetMenu(menuName = Constants.ContextMenus.Root + "Settings")]
    internal class AagenSettings : ScriptableObject
    {
        public List<InputFilterRule> _InputFilterRules;
        public List<MergeRule> _MergeRules;
        public List<GroupLayoutRule> _GroupLayoutRules;
    }
}
