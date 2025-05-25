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
        public AddressableAssetSettings AddressableSettings;

        public HashSet<AssetNode> IgnoredAssets;

        public Dictionary<int, SubgraphInfo> Subgraphs;
        public Dictionary<string, GroupLayoutInfo> GroupLayout;

        public bool AssetEditingInProgress;

        public SummaryReport SummaryReport = new SummaryReport();
        public Logger Logger;
        
        public Dictionary<SubgraphCategoryID, List<SubgraphInfo>> GetSubgraphsGroupedByCategory()
        {
            var output = new Dictionary<SubgraphCategoryID, List<SubgraphInfo>>();
            
            var subgraphs = Subgraphs.Values.ToList();
            
            //Select subgraphs by user-defined category IDs
            foreach (var categoryId in Settings.SubgraphCategoryIds)
            {
                output.Add(categoryId, subgraphs.Where(subgraph => subgraph.CategoryID == categoryId).ToList());
            }

            //Select subgraphs by default category IDs
            var defaultCategory = Settings.DefaultCategoryID;
            output.Add(defaultCategory, subgraphs.Where(subgraph => subgraph.CategoryID == defaultCategory).ToList());

            return output;
        }
    }
}
