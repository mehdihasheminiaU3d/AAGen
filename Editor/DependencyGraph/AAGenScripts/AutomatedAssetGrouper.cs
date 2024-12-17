using System;
using UnityEditor;
using UnityEngine;

namespace AAGen.Editor.DependencyGraph
{
    internal class AutomatedAssetGrouper : EditorWindow
    {
        [MenuItem("Tools/Dependency Graph/Automated Asset Grouper", priority = 300)]
        public static void ShowWindow()
        {
            GetWindow<AutomatedAssetGrouper>("Automated Asset Grouper");
        }

        private DependencyGraph _dependencyGraph;

        private EditorUiGroup _ignoreAssetsFileUi;
        private EditorUiGroup _groupCreatorUi;
        private EditorUiGroup _processGroupsUi;
        private EditorUiGroup _addressableGroupsUi;
        private EditorUiGroup _postProcessingUi;

        private Vector2 _scrollPosition;

        private DependencyGraphLoaderUi _dependencyGraphLoader;
        
        public AagSettings AagSettings { get; private set; }
        private EditorPersistentValue<string> _settingsAssetPath = new (null, "EPK_AAG_SettingsPath");

        private void OnEnable()
        {
            var assetPath = _settingsAssetPath.Value;
            if (!string.IsNullOrEmpty(assetPath))
            {
                AagSettings = AssetDatabase.LoadAssetAtPath<AagSettings>(assetPath);
            }
        }

        private void OnDisable()
        {
            if (AagSettings != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(AagSettings);
                _settingsAssetPath.Value = assetPath;
            }
            else
            {
                _settingsAssetPath.ClearPersistentData();
            }
        }

        private void OnGUI()
        {
            _dependencyGraphLoader ??= new DependencyGraphLoaderUi();
            _dependencyGraphLoader.OnGUI();
            _dependencyGraph = _dependencyGraphLoader.DependencyGraph;
            if (_dependencyGraph == null)
                return;

            AagSettings = (AagSettings)EditorGUILayout.ObjectField("Settings", AagSettings, typeof(AagSettings), false);
            if (AagSettings == null)
                return;

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(position.height));
            {
                _ignoreAssetsFileUi ??= CreateIgnoreAssetsFileUI();
                _ignoreAssetsFileUi.OnGUI();

                _groupCreatorUi ??= CreateSubgraphUI();
                _groupCreatorUi.OnGUI();

                _processGroupsUi ??= CreateGroupLayoutUI();
                _processGroupsUi.OnGUI();
                
                _addressableGroupsUi ??= CreateAddressableGroupsUI();
                _addressableGroupsUi.OnGUI();

                _postProcessingUi ??= CreatePostProcessingUI();
                _postProcessingUi.OnGUI();
            }
            EditorGUILayout.EndScrollView();
        }

        #region UI-Group Factory Methods

        private EditorUiGroup CreateIgnoreAssetsFileUI()
        {
            var uiGroup = new EditorUiGroup
            {
                FoldoutLabel = "Input Filter",
                UIVisibility = EditorUiGroup.UIVisibilityFlag.ShowFoldout |
                               EditorUiGroup.UIVisibilityFlag.ShowHelpBox |
                               EditorUiGroup.UIVisibilityFlag.ShowButton1 |
                               EditorUiGroup.UIVisibilityFlag.ShowOutput,
                HelpText = "Filters out unwanted assets (e.g., project settings) based on input filtering rules defined in the settings"
            };
            var processor = new PreProcessingFilter(_dependencyGraph, uiGroup, this);
            uiGroup.ButtonAction = processor.SaveIgnoredAssetsToFile;
            return uiGroup;
        }

        private EditorUiGroup CreateSubgraphUI()
        {
            var uiGroup = new EditorUiGroup
            {
                FoldoutLabel = "Subgraphs",
                UIVisibility = EditorUiGroup.UIVisibilityFlag.ShowFoldout |
                               EditorUiGroup.UIVisibilityFlag.ShowButton1 |
                               EditorUiGroup.UIVisibilityFlag.ShowHelpBox,
                HelpText = "Identifies subgraphs, which are collections of nodes sharing the same source set, to serve as the base unit for processing"
            };
            var processor = new SubgraphProcessor(_dependencyGraph, uiGroup);
            uiGroup.ButtonAction = processor.Execute;
            return uiGroup;
        }

        private EditorUiGroup CreateGroupLayoutUI()
        {
            var uiGroup = new EditorUiGroup
            {
                FoldoutLabel = "Group Layout",
                UIVisibility = EditorUiGroup.UIVisibilityFlag.ShowFoldout |
                               EditorUiGroup.UIVisibilityFlag.ShowHelpBox |
                               EditorUiGroup.UIVisibilityFlag.ShowButton1 |
                               EditorUiGroup.UIVisibilityFlag.ShowOutput,
                HelpText = "Organizes subgraphs into a group layout that serves as a blueprint for creating addressable asset groups"

            };
            var processor = new GroupLayoutProcessor(_dependencyGraph, uiGroup, this);
            uiGroup.ButtonAction = processor.Execute;
            return uiGroup;
        }

        private EditorUiGroup CreateAddressableGroupsUI()
        {
            var uiGroup = new EditorUiGroup
            {
                FoldoutLabel = "Addressable Groups",
                UIVisibility = EditorUiGroup.UIVisibilityFlag.ShowFoldout |
                               EditorUiGroup.UIVisibilityFlag.ShowHelpBox |
                               EditorUiGroup.UIVisibilityFlag.ShowButton1 |
                               EditorUiGroup.UIVisibilityFlag.ShowOutput,
                HelpText = "Creates addressable asset groups based on the defined group layout"
            };
            var processor = new AddressableGroupCreator(_dependencyGraph, uiGroup);
            uiGroup.ButtonAction = processor.Execute;
            return uiGroup;
        }
        
        private EditorUiGroup CreatePostProcessingUI()
        {
            var uiGroup = new EditorUiGroup
            {
                FoldoutLabel = "Cleanup",
                UIVisibility = EditorUiGroup.UIVisibilityFlag.ShowFoldout |
                               EditorUiGroup.UIVisibilityFlag.ShowHelpBox |
                               EditorUiGroup.UIVisibilityFlag.ShowButton1 |
                               EditorUiGroup.UIVisibilityFlag.ShowOutput,
                HelpText = "Moving assets to addressable groups may result in empty groups. This step removes them to enhance performance"

            };
            var processor = new PostProcessor(_dependencyGraph, uiGroup);
            uiGroup.ButtonAction = processor.Execute;
            return uiGroup;
        }
        #endregion
    }
}
