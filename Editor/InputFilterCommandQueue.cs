using System.Collections.Generic;
using System.Linq;
using AAGen.AssetDependencies;
using UnityEditor;

namespace AAGen
{
    internal class InputFilterCommandQueue : CommandQueue 
    {
        readonly DataContainer m_DataContainer;
        
        int m_NumUnsupportedFiles = 0;
        int m_NumIgnoredbyRule = 0;
        
        public InputFilterCommandQueue(DataContainer dataContainer) 
        {
            m_DataContainer = dataContainer;
            Title = nameof(InputFilterCommandQueue);
        }

        public override void PreExecute()
        {
            m_DataContainer.IgnoredAssets = new HashSet<AssetNode>();
            AddCommandsToIgnoreByInputRules();
            AddCommandsToIgnoreUnsupportedAssets();
            AddCommand(new ActionCommand(AddBuiltinScenesToIgnoredList));
            EnqueueCommands();
        }
        
        void AddCommandsToIgnoreByInputRules()
        {
            var allNodes = m_DataContainer.DependencyGraph.GetAllNodes();
            foreach (var node in allNodes)
            {
                AddCommand(new ActionCommand(() => AddRuledFileToIgnoredList(node), node.AssetPath));
            }
            
            AddCommand(new ActionCommand(() => m_DataContainer.SummaryReport.TryAdd($"Number of ignored files by rules = {m_NumIgnoredbyRule}")));
        }

        void AddRuledFileToIgnoredList(AssetNode node)
        {
            foreach (var inputFilterRule in m_DataContainer.Settings.InputFilterRules)
            {
                if (inputFilterRule.ShouldIgnoreNode(node, IsSource(node)))
                {
                    m_DataContainer.IgnoredAssets.Add(node);
                    m_NumIgnoredbyRule++;
                }
            }
        }
        
        bool IsSource(AssetNode node)
        {
            return m_DataContainer.TransposedGraph.GetNeighbors(node).Count == 0;
        }

        void AddCommandsToIgnoreUnsupportedAssets()
        {
            var allNodes = m_DataContainer.DependencyGraph.GetAllNodes();
            foreach (var node in allNodes)
            {
                AddCommand(new ActionCommand(() => AddUnsupportedFileToIgnoreAssets(node)));
            }
            
            AddCommand(new ActionCommand(() => m_DataContainer.SummaryReport.TryAdd($"Number of unsupported files ignored = {m_NumUnsupportedFiles}")));
        }
        
        void AddUnsupportedFileToIgnoreAssets(AssetNode node)
        {
            var mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(node.AssetPath);
            if (mainAssetType == null || mainAssetType == typeof(DefaultAsset))
            {
                m_DataContainer.IgnoredAssets.Add(node);
                m_NumUnsupportedFiles++;
            }
        }
        
        void AddBuiltinScenesToIgnoredList()
        {
            var scenes = EditorBuildSettings.scenes; //scenes in build profile
            if (scenes.Length == 0)
                return;
            
            var sceneNodes = scenes.Select(scene => AssetNode.FromAssetPath(scene.path)).ToHashSet();
            m_DataContainer.IgnoredAssets.UnionWith(sceneNodes);
            
            //We can use it here because it's a command
            m_DataContainer.SummaryReport.TryAdd($"Number of built-in scenes ignored = {scenes.Length}");
        }
    }
}