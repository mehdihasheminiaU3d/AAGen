using System.Collections.Generic;
using System.Linq;
using AAGen.AssetDependencies;
using UnityEditor;

namespace AAGen
{
    internal class IntakeFilterCommandQueue : CommandQueue 
    {
        readonly DataContainer m_DataContainer;
        
        public IntakeFilterCommandQueue(DataContainer dataContainer) 
        {
            m_DataContainer = dataContainer;
            m_DataContainer.IgnoredAssets = new HashSet<AssetNode>();
            Title = nameof(IntakeFilterCommandQueue);
        }

        public override void PreExecute()
        {
            AddCommandsToIgnoreByInputRules();
            AddCommandsToIgnoreUnsupportedAssets();
            AddCommand(new ActionCommand(AddBuiltinScenesToIgnoredList));
            EnqueueCommands();
        }
        
        void AddCommandsToIgnoreByInputRules()
        {
            var allNodes = m_DataContainer.m_DependencyGraph.GetAllNodes();
            foreach (var node in allNodes)
            {
                AddCommand(new ActionCommand(() => AddRuledFileToIgnoredList(node), node.AssetPath));
            }
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
            return m_DataContainer.m_TransposedGraph.GetNeighbors(node).Count == 0;
        }

        void AddCommandsToIgnoreUnsupportedAssets()
        {
            var allNodes = m_DataContainer.m_DependencyGraph.GetAllNodes();
            foreach (var node in allNodes)
            {
                AddCommand(new ActionCommand(() => AddUnsupportedFileToIgnoreAssets(node)));
            }
        }

        void AddUnsupportedFileToIgnoreAssets(AssetNode node)
        {
            var mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(node.AssetPath);
            if (mainAssetType == null || mainAssetType == typeof(DefaultAsset))
            {
                m_DataContainer.IgnoredAssets.Add(node);
            }
        }
        
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