using System.Collections.Generic;
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
        
        int m_EmptyGroupRemoved;
        int m_UnnecessaryEntriesRemoved;
        
        public override void PreExecute()
        {
            ClearQueue();
            AddCommand(StartAssetEditing);
            AddCommand(RemoveUnusedEntries);
            AddCommand(RemoveEmptyAddressableGroups);
            AddCommand(StopAssetEditing);
        }
        
        void RemoveUnusedEntries()
        {
            if (!m_DataContainer.Settings.RemoveUnnecessaryEntries)
                return;

            //List all assets used in group layouts
            var allNodesInGroupLayouts = new HashSet<string>();
            foreach (var groupLayout in m_DataContainer.GroupLayout.Values)
            {
                foreach (var node in groupLayout.Nodes)
                {
                    allNodesInGroupLayouts.Add(node.Guid.ToString());
                }
            }
            
            //Find entries that aren't included in group layout
            var entriesToRemove = new List<string>();
            foreach (var group in AddressableSettings.groups)
            {
                foreach (var entry in group.entries)
                {
                    var entryGuid = entry.guid;
                    
                    if(!allNodesInGroupLayouts.Contains(entryGuid))
                    {
                        entriesToRemove.Add(entryGuid);
                    }
                }
            }

            //Remove entries
            foreach (var guid in entriesToRemove)
            {
                AddressableSettings.RemoveAssetEntry(guid);
                m_UnnecessaryEntriesRemoved++;
            }
        }
        
        void RemoveEmptyAddressableGroups()
        {
            if (!m_DataContainer.Settings.RemoveEmptyGroups)
                return;
            
            var groups = AddressableSettings.groups.Where(CanRemoveGroup).ToList();
            
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
            summary += $"{nameof(m_EmptyGroupRemoved).ToReadableFormat()} = {m_EmptyGroupRemoved}\n";
            summary += $"{nameof(m_UnnecessaryEntriesRemoved).ToReadableFormat()} = {m_UnnecessaryEntriesRemoved}\n";
            
            m_DataContainer.SummaryReport.AppendLine(summary);
        }

        AddressableAssetSettings AddressableSettings => AddressableAssetSettingsDefaultObject.Settings;
    }
}