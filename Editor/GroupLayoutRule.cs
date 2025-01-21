using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace AAGen
{
    /// <summary>
    /// Contains data and logic to create group layouts for a category. If the category is not locked, it also breaks it into smaller chunks.
    /// </summary>
    internal abstract class GroupLayoutRule : ScriptableObject
    {
        [SerializeField,
        Tooltip("The category to which this rule applies")]
        public CategoryId _CategoryId;

        [SerializeField, Min(1f),
         Tooltip("Uncompressed size in megabytes")]
        private float _MaxSize = 10;

        [SerializeField]
        public AddressableAssetGroupTemplate _AddressableAssetGroupTemplate;

        public CategoryId CategoryId => _CategoryId;

        public string TemplateName => _AddressableAssetGroupTemplate != null ? _AddressableAssetGroupTemplate.Name : null;

        public float MaxSize => _MaxSize;
    }
}
