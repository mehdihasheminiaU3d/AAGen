using System;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace AAGen
{
    internal class AddressableGroupCommandQueue : CommandQueue
    {
        public AddressableGroupCommandQueue(DataContainer dataContainer) 
        {
            m_DataContainer = dataContainer;
            Title = nameof(AddressableGroupCommandQueue);
        }
        
        readonly DataContainer m_DataContainer;
        
        int m_AddressableGroupCreated;
        
        public override void PreExecute()
        {
            ClearQueue();
            
            AddCommand(StartAssetEditing);
            
            foreach (var pair in m_DataContainer.GroupLayout)
            {
                var groupName = pair.Key;
                var groupLayoutInfo = pair.Value;

                AddCommand(() => CreateGroupAndMoveAssets(groupName, groupLayoutInfo), groupName);
            }
            
            AddCommand(StopAssetEditing);
        }

        void CreateGroupAndMoveAssets(string groupName, GroupLayoutInfo groupLayoutInfo)
        {
            var addressableGroup = CreateGroupFromTemplate(groupName, groupLayoutInfo.TemplateName);
            m_AddressableGroupCreated++;

            foreach (var node in groupLayoutInfo.Nodes)
            {
                string assetGuid = node.Guid.ToString();
                if (string.IsNullOrEmpty(assetGuid))
                    throw new Exception($"Asset with path '{node.AssetPath}' not found in project.");

                var entry = AddressableSettings.CreateOrMoveEntry(assetGuid, addressableGroup, false, false);
                if (entry == null)
                    throw new Exception($"Failed to add asset '{node.AssetPath}' to group '{addressableGroup.name}'.");
            }
        }

        AddressableAssetGroup CreateGroupFromTemplate(string name, string templateName)
        {
            var template = FindTemplateByName(templateName);
            if (template == null)
                throw new Exception($"Template with name '{templateName}' not found!");

            AddressableAssetGroup newGroup = AddressableSettings.CreateGroup(name, false, false,
                false, null, typeof(BundledAssetGroupSchema));
            
            if (newGroup == null)
                throw new Exception($"Failed to create addressable group : {name}");
            
            template.ApplyToAddressableAssetGroup(newGroup);
            
            return newGroup;
        }
        
        AddressableAssetGroupTemplate FindTemplateByName(string templateName)
        {
            foreach (var template in AddressableSettings.GroupTemplateObjects)
            {
                if (template.name.Equals(templateName))
                {
                    return (AddressableAssetGroupTemplate)template;
                }
            }
            
            return null;
        }
        
        void StartAssetEditing()
        {
            AssetDatabase.StartAssetEditing();
            m_DataContainer.AssetEditingInProgress = true;
        }

        void StopAssetEditing()
        {
            AssetDatabase.StopAssetEditing();
            m_DataContainer.AssetEditingInProgress = false;
            AssetDatabase.Refresh();
        }
        
        public override void PostExecute()
        {
            AppendToSummaryReport();
        }

        void AppendToSummaryReport()
        {
            if (!m_DataContainer.Settings.GenerateSummaryReport)
                return;

            var summary = $"\n=== Addressable Groups ===\n";
            summary += $"{nameof(m_AddressableGroupCreated).ToReadableFormat()} = {m_AddressableGroupCreated}";
            
            m_DataContainer.SummaryReport.AppendLine(summary);
        }
        
        AddressableAssetSettings AddressableSettings => AddressableAssetSettingsDefaultObject.Settings;
    }
}