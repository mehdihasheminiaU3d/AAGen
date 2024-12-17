using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AAGen.Editor.DependencyGraph
{
    [CreateAssetMenu(menuName = "Dependency Graph/Automated Asset Grouping/" + nameof(IgnoreTestScenes))]
    internal class IgnoreTestScenes : InputFilterRule
    {
        [SerializeField, Tooltip("Ignores all scenes whose names are not included in this list." +
                                 " Built-in scenes are always ignored.\n")]
        private List<string> _IgnoreScenesExcept;
        
        public override bool ShouldIgnoreNode(AssetNode node)
        {
            var assetPath = node.AssetPath;
            var fileExtenstion = Path.GetExtension(assetPath);
            
            if (fileExtenstion.Equals(".unity", StringComparison.OrdinalIgnoreCase))
            {
                var fileName = Path.GetFileNameWithoutExtension(assetPath);
                foreach (var sceneName in _IgnoreScenesExcept)
                {
                    if (sceneName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                return true;
            }

            return false;
        }
    }
}
