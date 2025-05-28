using System.Collections.Generic;
using System.Linq;
using AAGen.AssetDependencies;
using UnityEditor.AddressableAssets.Settings;

namespace AAGen
{
    public class DataContainer
    {
        public DependencyGraph DependencyGraph;

        public string SettingsFilePath;
        public AagenSettings Settings;

        public HashSet<AssetNode> IgnoredAssets;

        public Dictionary<int, SubgraphInfo> Subgraphs;
        public Dictionary<string, GroupLayoutInfo> GroupLayout;

        public bool AssetEditingInProgress;

        public SummaryReport SummaryReport = new SummaryReport();
        public Logger Logger;
    }
}
