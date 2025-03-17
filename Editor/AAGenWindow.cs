using System;
using System.Collections;
using AAGen.AssetDependencies;
using AAGen.Shared;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace AAGen
{
    internal class AAGenWindow : EditorWindow, ISettingsHolderWindow
    {
        [MenuItem("Tools/AAGen", priority = 100)] //<-- ToDo: Add constants here 
        public static void ShowWindow()
        {
            var window = GetWindow<AAGenWindow>("AAGen");
            window.minSize = new Vector2(400, 200); 
        }
        
        public AagenSettings Settings { get; set; }
        EditorPersistentValue<string> _settingsAssetPath = new (null, "EPK_AAG_SettingsPath");
        
        const string k_BoxStyleName = "Box";
        const float k_Space = 5f;
        const string k_QuickButtonLabel = "Generate Addressable Groups Automatically";
        const int k_QuickButtonWidth = 300;
        const int k_QuickButtonHeight = 30;
        
        DependencyGraph m_DependencyGraph;
        DefaultSystemSetupCreator m_DefaultSystemSetupCreator;

        bool m_LoadingInProgress = false;
        
        void OnEnable()
        {
            var assetPath = _settingsAssetPath.Value;
            if (!string.IsNullOrEmpty(assetPath))
            {
                Settings = AssetDatabase.LoadAssetAtPath<AagenSettings>(assetPath);
            }
        }

        void OnDisable()
        {
            if (Settings != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(Settings);
                _settingsAssetPath.Value = assetPath;
            }
            else
            {
                _settingsAssetPath.ClearPersistentData();
            }
        }

        void OnGUI()
        {
            DrawQuickContainer();
        }
        
        void DrawQuickContainer()
        {
            GUILayout.BeginVertical(k_BoxStyleName);
            GUILayout.Space(k_Space);

            if (Settings != null)
            {
                DrawCentered(() =>
                {
                    Settings = (AagenSettings)EditorGUILayout.ObjectField(Settings, typeof(AagenSettings), false,
                        GUILayout.Width(k_QuickButtonWidth));
                }, k_QuickButtonWidth);
            }

            GUILayout.Space(k_Space);

            DrawCentered(() =>
            {
                if (GUILayout.Button(k_QuickButtonLabel, GUILayout.MinWidth(k_QuickButtonWidth), GUILayout.Height(k_QuickButtonHeight)))
                {
                    Execute();
                }
            }, k_QuickButtonWidth);

            GUILayout.Space(k_Space);
            GUILayout.EndVertical();
        }
        
        void Execute() 
        {
            var sequence = new EditorJobGroup(nameof(QuickButtonSequence));
            sequence.AddJob(new ActionJob(AddDefaultSettingsSequence, nameof(AddDefaultSettingsSequence)));
            sequence.AddJob(new ActionJob(Init, nameof(Init)));
            sequence.AddJob(new CoroutineJob(GenerateDependencyGraph, nameof(GenerateDependencyGraph)));
            sequence.AddJob(new CoroutineJob(LoadDependencyGraph, nameof(LoadDependencyGraph)));
            sequence.AddJob(new CoroutineJob(PreProcessScenes, nameof(PreProcessScenes)));
            sequence.AddJob(new CoroutineJob(PreProcess, nameof(PreProcess)));
            sequence.AddJob(new CoroutineJob(Subgraphs, nameof(Subgraphs)));
            sequence.AddJob(new CoroutineJob(GroupLayout, nameof(GroupLayout)));
            sequence.AddJob(new CoroutineJob(AddressableGroup, nameof(AddressableGroup)));
            sequence.AddJob(new CoroutineJob(PostProcessScenes, nameof(PostProcessScenes)));
            sequence.AddJob(new CoroutineJob(PostProcess, nameof(PostProcess)));
            EditorCoroutineUtility.StartCoroutineOwnerless(sequence.Run());
        }
        
        void Init()
        {
        }
        
        void AddDefaultSettingsSequence()
        {
            m_DefaultSystemSetupCreator = new DefaultSystemSetupCreator(m_DependencyGraph, null, this);
            m_DefaultSystemSetupCreator.CreateDefaultSettingsFiles();
        }

        IEnumerator GenerateDependencyGraph()
        {
            var dependencyGraphGenerator = new DependencyGraphGenerator();
            dependencyGraphGenerator.Start();
            yield return new WaitUntil(() => !dependencyGraphGenerator.InProgress);
        }
        
        IEnumerator LoadDependencyGraph()
        {
            if (m_LoadingInProgress)
                yield break;

            string filePath = Constants.DependencyGraphFilePath;
            m_LoadingInProgress = true;

            EditorCoroutineUtility.StartCoroutineOwnerless(FileUtils.LoadFromFileAsync<DependencyGraph>(
                filePath,
                (dependencyGraph) =>
                {
                    m_DependencyGraph = dependencyGraph;
                    m_LoadingInProgress = false;
                }));
        }
        
        IEnumerator PreProcessScenes()
        {
            var preProcessing = new PreProcessingScenes(m_DependencyGraph, null);
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(preProcessing.Execute());
        }
        
        IEnumerator PreProcess()
        {
            var preProcessingFilter = new PreProcessingFilter(m_DependencyGraph, null, this);
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(preProcessingFilter.SaveIgnoredAssetsToFile());
        }

        IEnumerator Subgraphs()
        {
            var subgraphProcessor = new SubgraphProcessor(m_DependencyGraph, null);
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(subgraphProcessor.Execute());
        }

        IEnumerator GroupLayout()
        {
            var groupLayoutProcessor = new GroupLayoutProcessor(m_DependencyGraph, null, this);
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(groupLayoutProcessor.Execute());
        }

        IEnumerator AddressableGroup()
        {
            var processor = new AddressableGroupCreator(m_DependencyGraph, null);
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(processor.Execute());
        }
        
        IEnumerator PostProcessScenes()
        {
            var processor = new PostProcessScenes(m_DependencyGraph, null);
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(processor.Execute());
        }
        
        IEnumerator PostProcess()
        {
            var processor = new PostProcessor(m_DependencyGraph, null);
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(processor.Execute());
        }
        
        /// <summary>
        /// Draws GUI elements centered horizontally within the available width.
        /// </summary>
        /// <param name="drawAction">Action that contains GUI drawing logic.</param>
        /// <param name="elementWidth">Width of the element to center.</param>
        void DrawCentered(Action drawAction, float elementWidth)
        {
            float padding = (position.width - elementWidth) / 2;

            GUILayout.BeginHorizontal();
            GUILayout.Space(padding);
    
            drawAction.Invoke(); // Execute the drawing logic
    
            GUILayout.Space(padding);
            GUILayout.EndHorizontal();
        }
    }
}