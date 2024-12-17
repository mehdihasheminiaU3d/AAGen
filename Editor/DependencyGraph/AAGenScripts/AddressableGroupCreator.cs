using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AAGen.Runtime;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace AAGen.Editor.DependencyGraph
{
    /// <summary>
    /// Creates addressable asset groups and assigns assets to them according to the specified group layout
    /// </summary>
    internal class AddressableGroupCreator : DependencyGraphProcessor
    {
        public AddressableGroupCreator(DependencyGraph dependencyGraph, EditorUiGroup uiGroup) 
            : base(dependencyGraph, uiGroup) {}
        
        private static string _filePath => Path.Combine(DependencyGraphConstants.FolderPath, "GroupLayout.txt");

        private EditorJobGroup _sequence;
        private AddressableAssetSettings _addressableSettings; 
        private Dictionary<string, GroupLayoutInfo> _groupLayout;
        private string _result;
        
        public void Execute()
        {
            _sequence = new EditorJobGroup(nameof(AddressableGroupCreator));
            _sequence.AddJob(new ActionJob(Init, nameof(Init)));
            _sequence.AddJob(new CoroutineJob(LoadGroupLayoutFromFile, nameof(LoadGroupLayoutFromFile)));
            _sequence.AddJob(new CoroutineJob(CreateAddressableGroups, nameof(CreateAddressableGroups)));
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
        
        private IEnumerator LoadGroupLayoutFromFile()
        {
            yield return DependencyGraphUtil.LoadFromFileAsync<Dictionary<string, GroupLayoutInfo>>(_filePath,
                (data) =>
                {
                    _groupLayout = data;
                });
        }

        private IEnumerator CreateAddressableGroups()
        {
            var startTime = EditorApplication.timeSinceStartup;
            int groupsCreated = 0;
            
            AssetDatabase.StartAssetEditing();
            
            foreach (var pair in _groupLayout)
            {
                var groupName = pair.Key;
                var groupLayoutInfo = pair.Value;
                AddressableAssetGroup addressableGroup = null;

                try
                {
                    addressableGroup = CreateGroupFromTemplate(groupName, groupLayoutInfo.TemplateName);
                    if (addressableGroup == null)
                        throw new Exception($"Failed to create addressable group : {groupName}");
                    
                    groupsCreated++;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    break;
                }

                bool internalLoopError = false;
                foreach (var node in groupLayoutInfo.Nodes)
                {
                    try
                    {
                        string assetGuid = node.Guid.ToString();
                        if (string.IsNullOrEmpty(assetGuid))
                            throw new Exception($"Asset with path '{node.AssetPath}' not found in project.");
                        
                        var entry = _addressableSettings.CreateOrMoveEntry(assetGuid, addressableGroup, false, false);
                        if (entry == null)
                            throw new Exception($"Failed to add asset '{node.AssetPath}' to group '{addressableGroup.name}'.");
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        internalLoopError = true;
                        break;
                    }
                    
                    if (ShouldUpdateUi)
                    {
                        _sequence.ReportProgress((float)groupsCreated / _groupLayout.Count, $"Group  created = {groupName}");
                        yield return null;
                    }
                }
                
                if(internalLoopError)
                    break;

                // Logs progress periodically to the console and process window during the long-running process to
                // reassure the user that the task is ongoing
                if (groupsCreated % 100 == 0)
                    Debug.Log($"{groupsCreated} Groups created so far. Most recent = {groupName} " +
                              $"| t={EditorApplication.timeSinceStartup - startTime:F2}s");
            }
            
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();

            Debug.Log($"{groupsCreated} Groups created in t={EditorApplication.timeSinceStartup - startTime:F2}s");
        }
        
        private AddressableAssetGroup CreateGroupFromTemplate(string name, string templateName)
        {
            var template = FindTemplateByName(templateName);
            if (template == null)
                throw new Exception($"Template with name '{templateName}' not found!");

            AddressableAssetGroup newGroup = _addressableSettings.CreateGroup(name, false, false,
                false, null, typeof(BundledAssetGroupSchema));
            template.ApplyToAddressableAssetGroup(newGroup);
            return newGroup;
        }
        
        private AddressableAssetGroupTemplate FindTemplateByName(string templateName)
        {
            foreach (var template in _addressableSettings.GroupTemplateObjects)
            {
                if (template.name.Equals(templateName))
                {
                    return (AddressableAssetGroupTemplate)template;
                }
            }
            
            return null;
        }
        
        void DisplayResultsOnUi()
        {
            _uiGroup.OutputText = _result;
        }
    }
}
