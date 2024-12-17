using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AAGen.Editor.DependencyGraph
{
    [CreateAssetMenu(menuName = "Dependency Graph/Automated Asset Grouping/" + nameof(IgnoreAssetByPathRule))]
    internal class IgnoreAssetByPathRule : InputFilterRule
    {
        [SerializeField, Tooltip("Ignores assets if their path does NOT contain any values from this list")]
        public List<string> _IgnorePathsExcept;
        
        [SerializeField, Tooltip("Ignores assets if their path contains any values from this list")]
        public List<string> _IgnorePaths;
        
        [SerializeField, Tooltip("Does not ignore assets if their path contains any values from this list")]
        public List<string> _DontIgnorePaths;
      
        public override bool ShouldIgnoreNode(AssetNode node)
        {
            var assetPath = node.AssetPath;

            if (_DontIgnorePaths.Any(path => node.AssetPath.Contains(path, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (_IgnorePathsExcept.Any(path => !assetPath.Contains(path, StringComparison.OrdinalIgnoreCase)))
                return true;
            
            if (_IgnorePaths.Any(path => assetPath.Contains(path, StringComparison.OrdinalIgnoreCase)))
                return true;
            
            return false;
        }
    }
}
