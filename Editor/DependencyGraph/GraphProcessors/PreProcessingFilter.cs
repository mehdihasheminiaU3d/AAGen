using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AAGen.Runtime;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace AAGen.Editor.DependencyGraph
{
    internal class PreProcessingFilter : DependencyGraphProcessor 
    {
        public PreProcessingFilter(DependencyGraph dependencyGraph, EditorUiGroup uiGroup, AutomatedAssetGrouper parentUi) : base(dependencyGraph, uiGroup)
        {
            _parentUi = parentUi;
        }
        
        private static string _filePath => Path.Combine(DependencyGraphConstants.FolderPath, "IgnoredAssets.txt");

        private AutomatedAssetGrouper _parentUi;
        private DependencyGraph _transposedGraph;
        private EditorJobGroup _sequence;
        private List<string> _ignoredFolders;
        private HashSet<AssetNode> _ignoredAssets;
        private string _result;
        
        public void SaveIgnoredAssetsToFile()
        {
            _sequence = new EditorJobGroup(nameof(GraphInfoProcessor));
            _sequence.AddJob(new ActionJob(Init, nameof(Init)));
            _sequence.AddJob(new CoroutineJob(IgnoreByInputRules, nameof(IgnoreByInputRules)));
            _sequence.AddJob(new CoroutineJob(IgnoreUnsupportedAssets, nameof(IgnoreUnsupportedAssets)));
            _sequence.AddJob(new CoroutineJob(IgnoreExclusiveDependencies, nameof(IgnoreExclusiveDependencies)));
            _sequence.AddJob(new CoroutineJob(SaveToFile, nameof(SaveToFile)));
            _sequence.AddJob(new ActionJob(DisplayResultsOnUi, nameof(DisplayResultsOnUi)));
            EditorCoroutineUtility.StartCoroutineOwnerless(_sequence.Run());
        }
        
        protected override void Init()
        {
            base.Init();
            _ignoredAssets = new HashSet<AssetNode>();
            _result = null;
            _transposedGraph = new DependencyGraph(_dependencyGraph.GetTransposedGraph());
        }
        
        private void DisplayResultsOnUi()
        {
            _uiGroup.OutputText = _result;
            _transposedGraph = null;
        }

        private IEnumerator IgnoreByInputRules()
        {
            var ignoredNodes = new HashSet<AssetNode>();

            var allNodes = _dependencyGraph.GetAllNodes();
            for (var i = 0; i < allNodes.Count; i++)
            {
                var node = allNodes[i];
                
                foreach (var inputFilterRule in _parentUi.AagSettings._InputFilterRules)
                {

                    if (inputFilterRule.IgnoreOnlySourceNodes)
                    {
                        if (inputFilterRule.ShouldIgnoreNode(node) && IsSource(node))
                            ignoredNodes.Add(node);
                    }
                    else
                    {
                        if (inputFilterRule.ShouldIgnoreNode(node))
                            ignoredNodes.Add(node);
                    }
                }

                if (ShouldUpdateUi)
                {
                    _sequence.ReportProgress((float)i / allNodes.Count, $"Input rules: {node.FileName}");
                    yield return null;
                }
            }
            
            _ignoredAssets.UnionWith(ignoredNodes);
        }

        private IEnumerator IgnoreUnsupportedAssets()
        {
            var ignoredNodes = new HashSet<AssetNode>();
            
            //Built-in scenes
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length > 0)
            {
                var sceneNodes = scenes.Select(scene => AssetNode.FromAssetPath(scene.path)).ToHashSet();
                ignoredNodes.UnionWith(sceneNodes);
            }
            
            //ignore unsupported files
            var allNodes = _dependencyGraph.GetAllNodes();
            for (var i = 0; i < allNodes.Count; i++)
            {
                var node = allNodes[i];
                
                //ignore unsupported files
                var mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(node.AssetPath);
                if (mainAssetType == null || mainAssetType == typeof(DefaultAsset))
                {
                    ignoredNodes.Add(node);
                    continue;
                }
                
                if (ShouldUpdateUi)
                {
                    _sequence.ReportProgress((float)i / allNodes.Count, $"Unsupported files: {node.FileName}");
                    yield return null;
                }
            }
            
            _ignoredAssets.UnionWith(ignoredNodes);
        }
        
        private IEnumerator IgnoreExclusiveDependencies()
        {
            _result = $"{_ignoredAssets.Count} assets will be ignored by AAG";
            yield break;
        }
        
        private bool IsSource(AssetNode node)
        {
            return _transposedGraph.GetNeighbors(node).Count == 0;
        }
        
        private IEnumerator SaveToFile()
        {
            yield return DependencyGraphUtil.SaveToFileAsync(_ignoredAssets, _filePath, (success) =>
            {
                if(!success)
                    Debug.LogError($">>> Failed to save ignored assets!");
            });
        }
    }
}
