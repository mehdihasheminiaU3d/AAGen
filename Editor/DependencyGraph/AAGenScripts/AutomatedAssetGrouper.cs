using System;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace AAGen.Editor.DependencyGraph
{
    internal class AutomatedAssetGrouper : EditorWindow
    {
        [MenuItem(Constants.Menus.AAGenMenuPath, priority = Constants.Menus.AAGenMenuPriority)]
        public static void ShowWindow()
        {
            var window = GetWindow<AutomatedAssetGrouper>("Automated Asset Grouper");
            window.minSize = new Vector2(600, 400); 
        }

        DependencyGraph _dependencyGraph;

        EditorUiGroup _defaultSettingsUi;
        EditorUiGroup _ignoreAssetsFileUi;
        EditorUiGroup _groupCreatorUi;
        EditorUiGroup _processGroupsUi;
        EditorUiGroup _addressableGroupsUi;
        EditorUiGroup _postProcessingUi;
        
        bool m_AdvancedModeActive;
        Vector2 _scrollPosition;
        DependencyGraphLoaderUi _dependencyGraphLoader;
        
        public AagSettings AagSettings { get; set; }
        EditorPersistentValue<string> _settingsAssetPath = new (null, "EPK_AAG_SettingsPath");

        GUIStyle m_SmallTextStyle;

        const string k_BoxStyleName = "Box";
        const string k_QuickButtonLabel = "Process Dependencies and Generate Addressable Groups";
        const int k_QuickButtonWidth = 450;
        const int k_QuickButtonHeight = 30;
        const string k_ModeButtonAdvancedLabel = "Switch to advanced mode";
        const string k_ModeButtonQuickLabel = "Switch to quick mode";
        const int k_ModeButtonWidth = 160;
        const int k_ModeButtonHeight = 20;
        const float k_Space = 10f;

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
            if (m_AdvancedModeActive)
            {
                DrawModeToggleButton();
                GUILayout.Space(k_Space);
                DrawAdvancedContainer();
            }
            else
            {
                DrawQuickContainer();
                GUILayout.Space(k_Space);
                DrawModeToggleButton();
            }
        }

        void DrawQuickContainer()
        {
            GUILayout.BeginVertical(k_BoxStyleName);
            GUILayout.Space(k_Space);

            if (AagSettings != null)
            {
                DrawCentered(() =>
                {
                    AagSettings = (AagSettings)EditorGUILayout.ObjectField(AagSettings, typeof(AagSettings), false,
                        GUILayout.Width(k_QuickButtonWidth));
                }, k_QuickButtonWidth);
            }

            GUILayout.Space(k_Space / 2f);

            DrawCentered(() =>
            {
                if (GUILayout.Button(k_QuickButtonLabel, GUILayout.MinWidth(k_QuickButtonWidth), GUILayout.Height(k_QuickButtonHeight)))
                {
                    new QuickButtonSequence(AagSettings,this).Execute();
                }
            }, k_QuickButtonWidth);

            GUILayout.Space(k_Space);
            GUILayout.EndVertical();
        }

        void DrawAdvancedContainer()
        {
            _dependencyGraphLoader ??= new DependencyGraphLoaderUi();
            _dependencyGraphLoader.OnGUI();
            _dependencyGraph = _dependencyGraphLoader.DependencyGraph;
            if (_dependencyGraph == null)
                return;

            AagSettings = (AagSettings)EditorGUILayout.ObjectField("Settings", AagSettings, typeof(AagSettings), false);

            float scrollViewHeight = position.height - GUILayoutUtility.GetLastRect().yMax; // Y position after top elements
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(scrollViewHeight));
            {
                if (AagSettings == null)
                {
                    _defaultSettingsUi ??= CreateDefaultSettingsUI();
                    _defaultSettingsUi.OnGUI();
                }
                else
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
            }
            EditorGUILayout.EndScrollView();
        }

        void DrawModeToggleButton()
        {
            m_SmallTextStyle ??= new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };
            
            string label = m_AdvancedModeActive ? k_ModeButtonQuickLabel : k_ModeButtonAdvancedLabel;
            if (GUILayout.Button(label, m_SmallTextStyle, GUILayout.MaxWidth(k_ModeButtonWidth), GUILayout.Height(k_ModeButtonHeight)))
            {
                m_AdvancedModeActive = !m_AdvancedModeActive;
            }
        }

        #region UI-Group Factory Methods
        
        private EditorUiGroup CreateDefaultSettingsUI()
        {
            var uiGroup = new EditorUiGroup
            {
                FoldoutLabel = "Default Settings",
                UIVisibility = EditorUiGroup.UIVisibilityFlag.ShowFoldout |
                               EditorUiGroup.UIVisibilityFlag.ShowHelpBox |
                               EditorUiGroup.UIVisibilityFlag.ShowButton1 ,
                HelpText = "Sets up the project and creates a default settings object"
            };
            var processor = new DefaultSystemSetupCreator(_dependencyGraph, uiGroup, this);
            uiGroup.ButtonAction = processor.CreateDefaultSettingsFiles;
            return uiGroup;
        }

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
            uiGroup.ButtonAction = () => EditorCoroutineUtility.StartCoroutineOwnerless(processor.SaveIgnoredAssetsToFile());
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
            uiGroup.ButtonAction = () => EditorCoroutineUtility.StartCoroutineOwnerless(processor.Execute());
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
            uiGroup.ButtonAction = () => EditorCoroutineUtility.StartCoroutineOwnerless(processor.Execute());
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
            uiGroup.ButtonAction = () => EditorCoroutineUtility.StartCoroutineOwnerless(processor.Execute());
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
            uiGroup.ButtonAction = () => EditorCoroutineUtility.StartCoroutineOwnerless(processor.Execute());
            return uiGroup;
        }
        #endregion

        #region Util

        /// <summary>
        /// Draws GUI elements centered horizontally within the available width.
        /// </summary>
        /// <param name="drawAction">Action that contains GUI drawing logic.</param>
        /// <param name="elementWidth">Width of the element to center.</param>
        private void DrawCentered(Action drawAction, float elementWidth)
        {
            float padding = (position.width - elementWidth) / 2;

            GUILayout.BeginHorizontal();
            GUILayout.Space(padding);
    
            drawAction.Invoke(); // Execute the drawing logic
    
            GUILayout.Space(padding);
            GUILayout.EndHorizontal();
        }

        #endregion
    }
}
