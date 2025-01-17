using System.IO;
using Unity.EditorCoroutines.Editor;
using UnityEditor;

namespace AAGen.Editor.DependencyGraph
{
    /// <summary>
    /// A UI group that provides an interface for loading the dependency graph from disk.
    /// This UI group is used in other editor windows to facilitate access to the dependency graph.
    /// It also provides crucial information about the recency and freshness of the dependency graph.
    /// </summary>
    internal class DependencyGraphLoaderUi : EditorUiGroup
    {
        public DependencyGraphLoaderUi() 
        {
            ButtonAction = LoadDependencyGraph;
        }
        
        public DependencyGraph DependencyGraph { get; private set; }
        bool _loadingInProgress;
        bool _fileExists;

        public override void OnGUI()
        {
            _fileExists = File.Exists(Constants.DependencyGraphFilePath);
            
            if(!_fileExists)
                UnloadLoadDependencyGraph();
            
            UpdateUIVisibility();
            base.OnGUI();
        }

        void UpdateUIVisibility()
        {
            UIVisibility = 0;

            //If the dependency graph isn't generated, disable load button
            if (!_fileExists)
            {
                HelpText = "Dependency graph cannot be found!";
                HelpMessageType = MessageType.Error;
                UIVisibility = UIVisibilityFlag.ShowHelpBox;
                return;
            }

            UIVisibility |= UIVisibilityFlag.ShowButton1;
            ButtonLabel = _loadingInProgress ? "Loading..." : "Load Dependency Graph";

            if (!AssetChangeDetectorService.HasChanges)
                return;
            
            //If change in assets are recorded, inform the user about it
            UIVisibility |= UIVisibilityFlag.ShowHelpBox;
            if (!AssetChangeDetectorService.HasMajorChanges)
            {
                HelpText = "Asset changes have been detected that aren't reflected in the dependency graph!";
                HelpMessageType = MessageType.Warning;
            }
            else
            {
                HelpText = "Major asset changes have been detected that aren't reflected in the dependency graph!\n" +
                           "Please re-generate the dependency graph.";
                HelpMessageType = MessageType.Error;
            }
        }
        
        void LoadDependencyGraph()
        {
            if (_loadingInProgress)
                return;
            
            string filePath = Constants.DependencyGraphFilePath;
            _loadingInProgress = true;
            
            EditorCoroutineUtility.StartCoroutineOwnerless(DependencyGraphUtil.LoadFromFileAsync<DependencyGraph>(filePath,
                (dependencyGraph) =>
                {
                    DependencyGraph = dependencyGraph;
                    _loadingInProgress = false;
                }));
        }

        void UnloadLoadDependencyGraph()
        {
            DependencyGraph = null;
        }
    }
}
