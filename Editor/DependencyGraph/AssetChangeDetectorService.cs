using System.Collections.Generic;
using AAGen.Shared;
using UnityEditor;

namespace AAGen
{
    /// <summary>
    /// Listens to editor events and monitors asset changes (such as importation, deletion, renaming, etc.) and records the number of changes.
    /// The recorded value can be used by other editor tools to validate the recency of the Dependency Graph.
    /// </summary>
    internal class AssetChangeDetectorService : AssetPostprocessor
    {
        private static EditorPersistentValue<int> _changeCount = new (0, "EPK_AssetChangeStatus");
        public static bool HasChanges => _changeCount.Value > 0;
        public static bool HasMajorChanges => _changeCount.Value > ProjectSettingsProvider.MajorChangeThreshold;
        
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            _changeCount.Value += CountAssetChanges(imported, deleted, moved, movedFrom);
        }

        private static int CountAssetChanges(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            var currentChanges = new HashSet<string>();

            foreach (string asset in imported)
            {
                if (!FileUtils.ShouldIgnoreAsset(asset))
                    currentChanges.Add(asset);
            }

            foreach (string asset in deleted)
            {
                if (!FileUtils.ShouldIgnoreAsset(asset))
                    currentChanges.Add(asset);
            }

            foreach (string asset in moved)
            {
                if (movedFrom.Length > 0 || !FileUtils.ShouldIgnoreAsset(asset))
                    currentChanges.Add(asset);
            }

            return currentChanges.Count;
        }
        
        public static void ResetChangeCount()
        {
            _changeCount.Value = 0;
        }
    }
}
