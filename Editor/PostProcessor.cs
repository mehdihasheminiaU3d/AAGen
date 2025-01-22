using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AAGen.AssetDependencies;
using AAGen.Shared;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace AAGen
{
    internal class PostProcessor : DependencyGraphProcessor
    {
        public PostProcessor(DependencyGraph dependencyGraph, EditorUiGroup uiGroup) : base(dependencyGraph, uiGroup)
        {
        }
        
        private string _result;
        private AddressableAssetSettings _addressableSettings;
        private EditorJobGroup _sequence;

        public IEnumerator Execute()
        {
            _sequence = new EditorJobGroup(nameof(AddressableGroupCreator));
            _sequence.AddJob(new ActionJob(Init, nameof(Init)));
            _sequence.AddJob(new CoroutineJob(RemoveEmptyAddressableGroups, nameof(RemoveEmptyAddressableGroups)));
            _sequence.AddJob(new ActionJob(DisplayResultsOnUi, nameof(DisplayResultsOnUi)));
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(_sequence.Run());
        }

        protected override void Init()
        {
            base.Init();
            
            _addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            if (_addressableSettings == null)
            {
                Debug.LogError("Addressable Asset Settings not found!");
                return;
            }
        }
        
        private IEnumerator RemoveEmptyAddressableGroups()
        {
            var startTime = EditorApplication.timeSinceStartup;
            
            List<AddressableAssetGroup> groups = _addressableSettings.groups.Where(CanRemoveGroup).ToList();
            if (ShouldUpdateUi)
                yield return null;

            _result += $"Groups to remove ({groups.Count}):\n{string.Join(",", groups.Select(group => group.Name))}";
            
            AssetDatabase.StartAssetEditing();
        
            try
            {
                foreach (var group in groups)
                {
                    _addressableSettings.RemoveGroup(group);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
            
            Debug.Log($"Empty groups removed in t={EditorApplication.timeSinceStartup - startTime:F2}s");

            bool CanRemoveGroup(AddressableAssetGroup group)
            {
                return group.entries.Count == 0 && 
                       !group.ReadOnly && 
                       group != _addressableSettings.DefaultGroup;
            }
        }
        
        private void DisplayResultsOnUi()
        {
            if (_uiGroup != null)
                _uiGroup.OutputText = _result;
        }
    }
}
