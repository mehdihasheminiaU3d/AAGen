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
        
        public override void PreExecute()
        {
            AddCommand(new ActionCommand(StartAssetEditing));
            AddCommand(new ActionCommand(RemoveEmptyAddressableGroups));
            AddCommand(new ActionCommand(StopAssetEditing));
            EnqueueCommands();
        }

        void RemoveEmptyAddressableGroups()
        {
            var groups = m_DataContainer.AddressableSettings.groups.Where(CanRemoveGroup).ToList();

            foreach (var group in groups)
            {
                m_DataContainer.AddressableSettings.RemoveGroup(group);
            }
        }

        bool CanRemoveGroup(AddressableAssetGroup group)
        {
            return group.entries.Count == 0 && 
                   !group.ReadOnly && 
                   group != m_DataContainer.AddressableSettings.DefaultGroup;
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