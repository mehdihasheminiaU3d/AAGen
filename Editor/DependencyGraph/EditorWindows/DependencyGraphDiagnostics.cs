using UnityEditor;

namespace AAGen
{
    /// <summary>
    /// An editor window that hosts tools for diagnosing asset inefficiencies based on dependency graph data.
    /// </summary>
    internal class DependencyGraphDiagnostics : EditorWindow
    {
        [MenuItem(Constants.Menus.DependencyGraphRootMenuPath + "Diagnostics",  priority = Constants.Menus.DependencyGraphMenuPriority + 2)]
        public static void ShowWindow()
        {
            GetWindow<DependencyGraphDiagnostics>("Diagnostic tools for DependencyGraph");
        }
        
        private DependencyGraph _dependencyGraph;
        private DependencyGraphLoaderUi _dependencyGraphLoader;
        private DependencyOverlapDetector _processor;
        
        private EditorUiGroup _hierarchyOverlapUi;
        private EditorUiGroup _indirectHierarchyOverlapUi;

        private void OnGUI()
        {
            _dependencyGraphLoader ??= new DependencyGraphLoaderUi();
            _dependencyGraphLoader.OnGUI();
            _dependencyGraph = _dependencyGraphLoader.DependencyGraph;
            if (_dependencyGraph == null)
                return;

            _hierarchyOverlapUi ??= CreateHierarchyOverlapUI();
            _hierarchyOverlapUi.OnGUI();

            _indirectHierarchyOverlapUi ??= CreateIndirectHierarchyOverlapUI();
            _indirectHierarchyOverlapUi.OnGUI();
        }

        #region UI-Group Factory Methods 
        private EditorUiGroup CreateHierarchyOverlapUI()
        {
            var uiGroup = new EditorUiGroup
            {
                FoldoutLabel = "Built-in Scenes Direct References to Addressable Assets",
                UIVisibility = EditorUiGroup.UIVisibilityFlag.ShowFoldout |
                               EditorUiGroup.UIVisibilityFlag.ShowHelpBox |
                               EditorUiGroup.UIVisibilityFlag.ShowButton1 |
                               EditorUiGroup.UIVisibilityFlag.ShowOutput,
                HelpText = "Analyzes the hierarchy of the built-in scenes and identifies addressable assets. \n" +
                           "These assets will be duplicated in the built-in bundle (.apk) and in bundles generated from" +
                           " addressable groups.",
            };
            
            _processor = new DependencyOverlapDetector(_dependencyGraph, uiGroup);
            uiGroup.ButtonAction = _processor.FindDirectOverlaps;
            return uiGroup;
        }
        
        private EditorUiGroup CreateIndirectHierarchyOverlapUI()
        {
            var uiGroup = new EditorUiGroup
            {
                FoldoutLabel = "Built-in Scenes Indirect References to Addressable Assets",
                UIVisibility = EditorUiGroup.UIVisibilityFlag.ShowFoldout |
                               EditorUiGroup.UIVisibilityFlag.ShowHelpBox |
                               EditorUiGroup.UIVisibilityFlag.ShowButton1 |
                               EditorUiGroup.UIVisibilityFlag.ShowOutput,
                HelpText = "Analyzes the hierarchy of the built-in scenes and identifies assets in the hierarchy that are" +
                           " not addressable themselves but are dependencies of other addressable assets.\n" +
                           "These assets will be duplicated in the built-in bundle (.apk) and in bundles generated from" +
                           " addressable groups.",
            };
            
            _processor = new DependencyOverlapDetector(_dependencyGraph, uiGroup);
            uiGroup.ButtonAction = _processor.FindIndirectOverlaps;
            return uiGroup;
        }
        #endregion
    }
}
