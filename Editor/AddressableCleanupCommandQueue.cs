using System;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace AAGen
{
    internal class AddressableCleanupCommandQueue : CommandQueue
    {
        public AddressableCleanupCommandQueue(DataContainer dataContainer) 
        {
            m_DataContainer = dataContainer;
            Title = nameof(AddressableCleanupCommandQueue);
        }
        
        readonly DataContainer m_DataContainer;
        AddressableAssetSettings m_AddressableSettings; 
        
        public override void PreExecute()
        {
            m_AddressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            if (m_AddressableSettings == null)
                throw new Exception($"Addressable Asset Settings not found!");
            
            AddCommand(new ActionCommand(StartAssetEditing));
            AddCommand(new ActionCommand(RemoveEmptyAddressableGroups));
            AddCommand(new ActionCommand(StopAssetEditing));
            EnqueueCommands();
        }

        void RemoveEmptyAddressableGroups()
        {
            var groups = m_AddressableSettings.groups.Where(CanRemoveGroup).ToList();

            foreach (var group in groups)
            {
                m_AddressableSettings.RemoveGroup(group);
            }
        }

        bool CanRemoveGroup(AddressableAssetGroup group)
        {
            return group.entries.Count == 0 && 
                   !group.ReadOnly && 
                   group != m_AddressableSettings.DefaultGroup;
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
    }
}