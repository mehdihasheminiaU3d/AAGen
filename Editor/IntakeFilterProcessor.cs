using System.Collections.Generic;
using System.Linq;
using AAGen.AssetDependencies;
using UnityEditor;

namespace AAGen
{
    internal class IntakeFilterProcessor : NodeProcessor 
    {
        public IntakeFilterProcessor(DataContainer dataContainer) 
        {
            m_DataContainer = dataContainer;
            m_DataContainer.IgnoredAssets = new HashSet<AssetNode>();
            m_TransposedGraph = new DependencyGraph(dataContainer.m_DependencyGraph.GetTransposedGraph());
            
            var root = new ProcessingUnit(null) { Name = "Root" };
            
            root.AddChild(IgnoreByInputRules());
            root.AddChild(IgnoreUnsupportedAssets());
            root.AddChild(new ProcessingUnit(AddBuiltinScenesToIgnoredList));
            SetRoot(root);
        }
        
        DependencyGraph m_TransposedGraph;
        DataContainer m_DataContainer;
        
        //---------------------------------------------------------------------------------------------------------------
        
        ProcessingUnit IgnoreByInputRules()
        {
            var root = new ProcessingUnit(null) { Name = "IgnoreByInputRules" };
            
            var allNodes = m_DataContainer.m_DependencyGraph.GetAllNodes();
            foreach (var node in allNodes)
            {
                root.AddChild(new ProcessingUnit(() => AddRuledFileToIgnoredList(node)));
            }

            return root;
        }

        void AddRuledFileToIgnoredList(AssetNode node)
        {
            foreach (var inputFilterRule in m_DataContainer.Settings._InputFilterRules)
            {

                if (inputFilterRule.IgnoreOnlySourceNodes)
                {
                    if (inputFilterRule.ShouldIgnoreNode(node) && IsSource(node))
                        m_DataContainer.IgnoredAssets.Add(node);
                }
                else
                {
                    if (inputFilterRule.ShouldIgnoreNode(node))
                        m_DataContainer.IgnoredAssets.Add(node);
                }
            }
        }
        
        bool IsSource(AssetNode node)
        {
            return m_TransposedGraph.GetNeighbors(node).Count == 0;
        }
        
        //----------------------------------------------------------------------------------------------------------------

        ProcessingUnit IgnoreUnsupportedAssets()
        {
            var root = new ProcessingUnit(null) { Name = "IgnoreUnsupportedAssets" };
            
            var allNodes = m_DataContainer.m_DependencyGraph.GetAllNodes();
            foreach (var node in allNodes)
            {
                root.AddChild(new ProcessingUnit(() => AddUnsupportedFileToIgnoreAssets(node)));
            }

            return root;
        }

        void AddUnsupportedFileToIgnoreAssets(AssetNode node)
        {
            var mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(node.AssetPath);
            if (mainAssetType == null || mainAssetType == typeof(DefaultAsset))
            {
                m_DataContainer.IgnoredAssets.Add(node);
            }
        }
        
        //----------------------------------------------------------------------------------------------------------------
        
        void AddBuiltinScenesToIgnoredList()
        {
            var scenes = EditorBuildSettings.scenes; //scenes in build profile
            if (scenes.Length == 0)
                return;
            
            var sceneNodes = scenes.Select(scene => AssetNode.FromAssetPath(scene.path)).ToHashSet();
            m_DataContainer.IgnoredAssets.UnionWith(sceneNodes);
        }
    }
}