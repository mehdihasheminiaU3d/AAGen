﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AAGen.Runtime;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace AAGen.Editor.DependencyGraph
{
    internal class PostProcessor : DependencyGraphProcessor
    {
        public PostProcessor(DependencyGraph dependencyGraph, EditorUiGroup uiGroup) : base(dependencyGraph, uiGroup)
        {
        }
        
        private string _result;
        private AddressableAssetSettings _addressableSettings;
        private EditorJobGroup _sequence;

        public void Execute()
        {
            _sequence = new EditorJobGroup(nameof(AddressableGroupCreator));
            _sequence.AddJob(new ActionJob(Init, nameof(Init)));
            _sequence.AddJob(new CoroutineJob(RemoveEmptyAddressableGroups, nameof(RemoveEmptyAddressableGroups)));
            _sequence.AddJob(new ActionJob(DisplayResultsOnUi, nameof(DisplayResultsOnUi)));
            EditorCoroutineUtility.StartCoroutineOwnerless(_sequence.Run());
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
            
            List<AddressableAssetGroup> groups = _addressableSettings.groups.Where(group => !group.ReadOnly && group.entries.Count == 0).ToList();
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
        }
        
        private void DisplayResultsOnUi()
        {
            _uiGroup.OutputText = _result;
        }
    }
}
