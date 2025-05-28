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
        
        int m_EmptyGroupCount;
        int m_EmptyGroupRemoved;
        
        public override void PreExecute()
        {
            ClearQueue();
            AddCommand(StartAssetEditing);
            AddCommand(RemoveEmptyAddressableGroups);
            AddCommand(StopAssetEditing);
        }

        void RemoveEmptyAddressableGroups()
        {
            var groups = AddressableSettings.groups.Where(CanRemoveGroup).ToList();
            m_EmptyGroupCount = groups.Count;
            
            foreach (var group in groups)
            {
                AddressableSettings.RemoveGroup(group);
                m_EmptyGroupRemoved++;
            }
        }

        bool CanRemoveGroup(AddressableAssetGroup group)
        {
            return group.entries.Count == 0 && 
                   !group.ReadOnly && 
                   group != AddressableSettings.DefaultGroup;
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

            var summary = $"\n=== Addressable Group Cleanup ===\n";
            summary += $"{nameof(m_EmptyGroupCount).ToReadableFormat()} = {m_EmptyGroupCount} \n";
            summary += $"{nameof(m_EmptyGroupRemoved).ToReadableFormat()} = {m_EmptyGroupRemoved}\n";
            
            m_DataContainer.SummaryReport.AppendLine(summary);
        }

        AddressableAssetSettings AddressableSettings => AddressableAssetSettingsDefaultObject.Settings;
    }
}