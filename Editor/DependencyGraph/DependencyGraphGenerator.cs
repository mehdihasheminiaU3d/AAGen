﻿using System;
using System.IO;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace AAGen.Editor.DependencyGraph
{
    /// <summary>
    /// Generates a dependency graph and stores it on disk.
    /// </summary>
    internal class DependencyGraphGenerator
    {
        private string _filePath => DependencyGraphConstants.DependencyGraphFilePath;

        private bool _isCancelled;
        private EditorUiGroup _uiGroup;

        public void Start()
        {
            ResetProgressBar();
            CreateGraph();
        }

        public void Cancel()
        {
            _isCancelled = true;
        }

        private void CreateGraph()
        {
            StartProgressBar("Generate Dependency Graph");

            var assetPaths = AssetDatabase.GetAllAssetPaths();
            DependencyGraph dependencyGraph = new DependencyGraph();

            var targetFrameTime = DependencyGraphSettings.TargetEditorFrameTime;

            for (int i = 0; i < assetPaths.Length; i++)
            {
                string assetPath = assetPaths[i];
                var iterationStartTime = EditorApplication.timeSinceStartup;

                try
                {
                    AddAssetToGraph(dependencyGraph, assetPath);
                }
                catch (Exception ex)
                {
                    OnFailure(ex.Message);
                    break;
                }

                if (EditorApplication.timeSinceStartup - iterationStartTime > targetFrameTime)
                {
                    UpdateProgressBar((float)i / assetPaths.Length,
                        $"Analyzing Dependencies of {Path.GetFileName(assetPath)}");
                }

                if (_isCancelled)
                    break;
            }

            if (_isCancelled)
            {
                OnFailure("Cancelled!");
                return;
            }

            EditorCoroutineUtility.StartCoroutineOwnerless(DependencyGraphUtil.SaveToFileAsync(dependencyGraph,
                _filePath, success =>
                {
                    if (success)
                        OnSuccess();
                    else
                        OnFailure($"Cannot save to {_filePath}");
                }));
        }

        private void StartProgressBar(string title)
        {
            ResetProgressBar();
            UpdateProgressBar(0, null);
        }

        private void UpdateProgressBar(float progress, string info)
        {
            if (EditorUtility.DisplayCancelableProgressBar("Dependency Graph Generator", info, progress))
                _isCancelled = true;
        }

        private void ResetProgressBar()
        {
            EditorUtility.ClearProgressBar();
            _isCancelled = false;
        }

        private void OnSuccess()
        {
            ResetProgressBar();
            AssetChangeDetectorService.ResetChangeCount();
        }

        private void OnFailure(string msg)
        {
            ResetProgressBar();
            Debug.LogError($"Dependency graph isn't created. Reason : {msg}");
        }
        
        private void AddAssetToGraph(DependencyGraph dependencyGraph, string assetPath)
        {
            if (DependencyGraphUtil.ShouldIgnoreAsset(assetPath))
                return;

            var dependencies = AssetDatabase.GetDependencies(assetPath, false);

            if (dependencies == null || dependencies.Length == 0)
            {
                dependencyGraph.AddNode(assetPath);
                return;
            }

            foreach (var dependency in dependencies)
            {
                if (DependencyGraphUtil.ShouldIgnoreAsset(dependency))
                    continue;

                dependencyGraph.AddEdge(assetPath, dependency);
            }
        }
    }
}
