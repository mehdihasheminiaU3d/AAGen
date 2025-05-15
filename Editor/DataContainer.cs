using System.Collections.Generic;
using AAGen.AssetDependencies;
using UnityEditor.AddressableAssets.Settings;

namespace AAGen
{
    public class DataContainer
    {
        public DependencyGraph DependencyGraph;
        public DependencyGraph TransposedGraph;

        public string SettingsFilePath;
        public AagenSettings Settings;
        public AddressableAssetSettings AddressableSettings;

        public HashSet<AssetNode> IgnoredAssets;

        public Category Subgraphs;
        public Dictionary<int, HashSet<AssetNode>> SubgraphSources;

        public Dictionary<string, GroupLayoutInfo> GroupLayout;

        public bool AssetEditingInProgress;

        public SummaryReport SummaryReport = new SummaryReport();
    }
}