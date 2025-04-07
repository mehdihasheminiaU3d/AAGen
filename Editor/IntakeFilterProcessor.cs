using System.Collections;
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
            _transposedGraph = new DependencyGraph(dataContainer.m_DependencyGraph.GetTransposedGraph());
            
            var root = new ProcessingUnit(null) { Name = "Root" };
            
            root.AddChild(IgnoreByInputRules());
            root.AddChild(IgnoreUnsupportedAssets());
            root.AddChild(new ProcessingUnit(AddBuiltinScenesToIgnoredList));
            SetRoot(root);
        }
        
        DependencyGraph _transposedGraph;
        List<string> _ignoredFolders;
        HashSet<AssetNode> _ignoredAssets;
        DataContainer m_DataContainer;
        
        //---------------------------------------------------------------------------------------------------------------
        
        ProcessingUnit IgnoreByInputRules()
        {
            var root = new ProcessingUnit(null) { Name = "IgnoreByInputRules" };
            
            var allNodes = m_DataContainer.m_DependencyGraph.GetAllNodes();
            foreach (var node in allNodes)
            {
                root.AddChild(new IgnoreByInputRulesProcessingUnit(node,
                    m_DataContainer.Settings._InputFilterRules,
                    m_DataContainer.IgnoredAssets,
                    _transposedGraph));
            }

            return root;
        }
        
        class IgnoreByInputRulesProcessingUnit : ProcessingNode
        {
            readonly DependencyGraph m_TransposedGraph;
            readonly AssetNode m_Node;
            readonly List<InputFilterRule> m_InputFilterRules;
            readonly HashSet<AssetNode> m_IgnoredAssets;

            public IgnoreByInputRulesProcessingUnit(AssetNode node, List<InputFilterRule> inputFilterRules, HashSet<AssetNode> ignoredAssets, DependencyGraph transposedGraph)
            {
                m_Node = node;
                m_InputFilterRules = inputFilterRules;
                m_IgnoredAssets = ignoredAssets;
                m_TransposedGraph = transposedGraph;
            }

            protected override void OnProcess()
            {
                foreach (var inputFilterRule in m_InputFilterRules)
                {

                    if (inputFilterRule.IgnoreOnlySourceNodes)
                    {
                        if (inputFilterRule.ShouldIgnoreNode(m_Node) && IsSource(m_Node))
                            m_IgnoredAssets.Add(m_Node);
                    }
                    else
                    {
                        if (inputFilterRule.ShouldIgnoreNode(m_Node))
                            m_IgnoredAssets.Add(m_Node);
                    }
                }
            }
            
            private bool IsSource(AssetNode node)
            {
                return m_TransposedGraph.GetNeighbors(node).Count == 0;
            }
        }
        
        //----------------------------------------------------------------------------------------------------------------

        ProcessingUnit IgnoreUnsupportedAssets()
        {
            var root = new ProcessingUnit(null) { Name = "IgnoreUnsupportedAssets" };
            
            var allNodes = m_DataContainer.m_DependencyGraph.GetAllNodes();
            foreach (var node in allNodes)
            {
                root.AddChild(new IgnoreUnsupportedAssetsProcessingUnit(node, m_DataContainer.IgnoredAssets));
            }

            return root;
        }

        class IgnoreUnsupportedAssetsProcessingUnit : ProcessingNode
        {
            readonly AssetNode m_Node;
            readonly HashSet<AssetNode> m_IgnoredAssets;

            public IgnoreUnsupportedAssetsProcessingUnit(AssetNode node, HashSet<AssetNode> ignoredAssets)
            {
                m_Node = node;
                m_IgnoredAssets = ignoredAssets;
            }

            protected override void OnProcess()
            {
                var mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(m_Node.AssetPath);
                if (mainAssetType == null || mainAssetType == typeof(DefaultAsset))
                {
                    m_IgnoredAssets.Add(m_Node);
                }
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