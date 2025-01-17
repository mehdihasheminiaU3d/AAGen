using UnityEditor;

namespace AAGen.Editor.DependencyGraph
{
    internal class DependencyGraphGeneratorWindow : EditorWindow
    {
        [MenuItem(Constants.Menus.DependencyGraphRootMenuPath + "Generate",  priority = Constants.Menus.DependencyGraphMenuPriority)]
        public static void ShowWindow()
        {
            GetWindow<DependencyGraphGeneratorWindow>("Generate DependencyGraph");
        }

        private EditorUiGroup _dependencyGraphGeneratorUi;
        
        private void OnGUI()
        {
            _dependencyGraphGeneratorUi ??= CreateDependencyGraphGeneratorUI();
            _dependencyGraphGeneratorUi.OnGUI();
        }

        #region UI-Group Factory Methods  
        private EditorUiGroup CreateDependencyGraphGeneratorUI()
        {
            var uiGroup = new EditorUiGroup
            {
                UIVisibility = EditorUiGroup.UIVisibilityFlag.ShowButton1 |
                               EditorUiGroup.UIVisibilityFlag.ShowOutput,
                ButtonLabel = "Generate Dependency Graph",
                
            };
            var processor = new DependencyGraphGenerator();
            uiGroup.ButtonAction = processor.Start;
            return uiGroup;
        }
        #endregion
    }
}
