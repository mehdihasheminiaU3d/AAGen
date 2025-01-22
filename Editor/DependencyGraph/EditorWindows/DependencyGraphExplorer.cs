using UnityEditor;
using AAGen.Shared;

namespace AAGen
{
    /// <summary>
    /// An editor window that hosts tools for exploring asset dependencies using dependency graph data.
    /// </summary>
    internal class DependencyGraphExplorer : EditorWindow
    {
        [MenuItem(Constants.Menus.DependencyGraphRootMenuPath + "Explore" ,  priority = Constants.Menus.DependencyGraphMenuPriority + 1)]
        public static void ShowWindow()
        {
            GetWindow<DependencyGraphExplorer>("Explore DependencyGraph");
        }

        private DependencyGraph _dependencyGraph;
        private DependencyGraphLoaderUi _dependencyGraphLoader;
        
        private EditorUiGroup _disaplySourceNodesUi;
        private EditorUiGroup _disaplySinkNodesUi;
        private EditorUiGroup _displayPathsUi;
        private EditorUiGroup _displayComponentsUi;

        private void OnGUI()
        {
            _dependencyGraphLoader ??= new DependencyGraphLoaderUi();
            _dependencyGraphLoader.OnGUI();
            _dependencyGraph = _dependencyGraphLoader.DependencyGraph;
            if (_dependencyGraph == null)
                return;

            _disaplySourceNodesUi ??= CreateDisplaySourceNodesUI();
            _disaplySourceNodesUi.OnGUI();

            _disaplySinkNodesUi ??= CreateDisplaySinkNodesUI();
            _disaplySinkNodesUi.OnGUI();
            
            _displayPathsUi ??= CreateDisplayPathsUI();
            _displayPathsUi.OnGUI();

            _displayComponentsUi ??= CreateDisplayComponentsUI();
            _displayComponentsUi.OnGUI();
        }

        #region UI-Group Factory Methods 
        private EditorUiGroup CreateDisplaySourceNodesUI()
        {
            var uiGroup = new EditorUiGroup
            {
                FoldoutLabel = "Find Source Nodes",
                UIVisibility = EditorUiGroup.UIVisibilityFlag.ShowFoldout |
                               EditorUiGroup.UIVisibilityFlag.ShowObjectFiled1 |
                               EditorUiGroup.UIVisibilityFlag.ShowHelpBox |
                               EditorUiGroup.UIVisibilityFlag.ShowButton1 |
                               EditorUiGroup.UIVisibilityFlag.ShowOutput,
                ObjectFieldLabel1 = "Target Asset",
                HelpText = "Finds the source nodes in the hierarchy of an asset. Source nodes are the roots of the hierarchy.\n" +
                           "They depend on other assets but are not dependencies of any other asset.\n" +
                           "In the graph, they are represented by nodes that have only outgoing edges.",

            };
            var processor = new GraphInfoProcessor(_dependencyGraph, uiGroup);
            uiGroup.ButtonAction = processor.FindSourceNodes;
            return uiGroup;
        }

        private EditorUiGroup CreateDisplaySinkNodesUI()
        {
            var uiGroup = new EditorUiGroup
            {
                FoldoutLabel = "Find Sink Nodes",
                UIVisibility = EditorUiGroup.UIVisibilityFlag.ShowFoldout |
                               EditorUiGroup.UIVisibilityFlag.ShowObjectFiled1 |
                               EditorUiGroup.UIVisibilityFlag.ShowHelpBox |
                               EditorUiGroup.UIVisibilityFlag.ShowButton1 |
                               EditorUiGroup.UIVisibilityFlag.ShowOutput,
                ObjectFieldLabel1 = "Target Asset",
                HelpText = "Finds the sink nodes in the hierarchy of an asset. Sink nodes are the leaves of the hierarchy.\n" +
                           "They are dependencies of other assets in the hierarchy but do not depend on any other asset themselves.\n" +
                           "In the graph, they are represented by nodes that have only incoming edges.",
            };
            var processor = new GraphInfoProcessor(_dependencyGraph, uiGroup);
            uiGroup.ButtonAction = processor.FindSinkNodes;
            return uiGroup;
        }

        private EditorUiGroup CreateDisplayComponentsUI()
        {
            var uiGroup = new EditorUiGroup
            {
                FoldoutLabel = "Display components",
                UIVisibility = EditorUiGroup.UIVisibilityFlag.ShowFoldout |
                               EditorUiGroup.UIVisibilityFlag.ShowHelpBox |
                               EditorUiGroup.UIVisibilityFlag.ShowButton1 |
                               EditorUiGroup.UIVisibilityFlag.ShowOutput,
                HelpText = "Identifies connected components in a graph. A connected component is a subset of nodes where each node is reachable from any other node in the subset.",
            };

            var processor = new GraphInfoProcessor(_dependencyGraph, uiGroup);
            uiGroup.ButtonAction = processor.FindConnectedComponents;
            return uiGroup;
        }
        
        private EditorUiGroup CreateDisplayPathsUI()
        {
            var uiGroup = new EditorUiGroup
            {
                FoldoutLabel = "Display paths between two assets",
                UIVisibility = EditorUiGroup.UIVisibilityFlag.ShowFoldout |
                               EditorUiGroup.UIVisibilityFlag.ShowObjectFiled1 |
                               EditorUiGroup.UIVisibilityFlag.ShowObjectFiled2 |
                               EditorUiGroup.UIVisibilityFlag.ShowHelpBox |
                               EditorUiGroup.UIVisibilityFlag.ShowButton1 |
                               EditorUiGroup.UIVisibilityFlag.ShowOutput,
                ObjectFieldLabel1 = "From",
                ObjectFieldLabel2 = "To",
                HelpText = "Finds all dependency paths between two assets.",
            };
            var processor = new GraphInfoProcessor(_dependencyGraph, uiGroup);
            uiGroup.ButtonAction = processor.FindPaths;
            return uiGroup;
        }
        #endregion
    }
}
