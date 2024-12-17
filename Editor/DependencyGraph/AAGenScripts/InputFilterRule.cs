using UnityEngine;

namespace AAGen.Editor.DependencyGraph
{
    /// <summary>
    /// Logic and data to define a rule to filter out unwanted assets
    /// to prevent them from being included in the final set of addressable assets
    /// </summary>
    internal abstract class InputFilterRule : ScriptableObject
    {
        [SerializeField,
         Tooltip("Ignores the specified nodes in the rule only if they're source nodes")]
        public bool _IgnoreOnlySourceNodes = true;

        public bool IgnoreOnlySourceNodes => _IgnoreOnlySourceNodes;

        public abstract bool ShouldIgnoreNode(AssetNode node);
    }
}
