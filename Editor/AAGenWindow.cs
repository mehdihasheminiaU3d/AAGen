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

        void LoadSettingsFile()
        {
            Settings = AssetDatabase.LoadAssetAtPath<AagenSettings>(m_DataContainer.SettingsFilePath);
        }

        void OnGUI()
        {
            DrawQuickContainer();
        }
        
        void DrawQuickContainer()
        {
            GUILayout.BeginVertical(k_BoxStyleName);
            GUILayout.Space(k_Space);

            // if (Settings != null)
            // {
                DrawCentered(() =>
                {
                    Settings = (AagenSettings)EditorGUILayout.ObjectField(Settings, typeof(AagenSettings), false,
                        GUILayout.Width(k_QuickButtonWidth));
                }, k_QuickButtonWidth);
            // }

            GUILayout.Space(k_Space);

            DrawCentered(() =>
            {
                if (GUILayout.Button(k_QuickButtonLabel, GUILayout.MinWidth(k_QuickButtonWidth), GUILayout.Height(k_QuickButtonHeight)))
                {
                    // EditorCoroutineUtility.StartCoroutineOwnerless(Execute());
                    ExecuteBlocking();
                }
            }, k_QuickButtonWidth);

            GUILayout.Space(k_Space);
            GUILayout.EndVertical();
        }
        
        IEnumerator Execute()
        {
            m_DefaultSystemSetupCreator = new DefaultSystemSetupCreator(m_DependencyGraph, null, this);
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(m_DefaultSystemSetupCreator.CreateSequence().Run());
            //Wait for the settings to be found/created
            
            var sequence = new EditorJobGroup(nameof(QuickButtonSequence));
            
            sequence.AddJob(new ActionJob(Setup, nameof(Setup)));

            if (Settings.ProcessingSteps.HasFlag(ProcessingStepID.GenerateDependencyGraph))
                sequence.AddJob(new CoroutineJob(GenerateDependencyGraph, nameof(GenerateDependencyGraph)));

            sequence.AddJob(new CoroutineJob(LoadDependencyGraph, nameof(LoadDependencyGraph)));

            if (Settings.ProcessingSteps.HasFlag(ProcessingStepID.RemoveScenesFromBuildProfile))
                sequence.AddJob(new CoroutineJob(RemoveScenesFromBuildProfile, nameof(RemoveScenesFromBuildProfile)));

            if (Settings.ProcessingSteps.HasFlag(ProcessingStepID.AssetIntakeFilter))
                sequence.AddJob(new CoroutineJob(GenerateIntakeFilter, nameof(GenerateIntakeFilter)));

            if (Settings.ProcessingSteps.HasFlag(ProcessingStepID.GenerateSubGraphs))
                sequence.AddJob(new CoroutineJob(GenerateSubgraphs, nameof(GenerateSubgraphs)));

            if (Settings.ProcessingSteps.HasFlag(ProcessingStepID.GenerateGroupLayout))
                sequence.AddJob(new CoroutineJob(GenerateGroupLayout, nameof(GenerateGroupLayout)));

            if (Settings.ProcessingSteps.HasFlag(ProcessingStepID.GenerateAddressableGroups))
                sequence.AddJob(new CoroutineJob(GenerateAddressableGroup, nameof(GenerateAddressableGroup)));

            if (Settings.ProcessingSteps.HasFlag(ProcessingStepID.RemoveScenesFromBuildProfile))
                sequence.AddJob(new CoroutineJob(AddAndSetupBootScene, nameof(AddAndSetupBootScene)));

            if (Settings.ProcessingSteps.HasFlag(ProcessingStepID.Cleanup))
                sequence.AddJob(new CoroutineJob(Cleanup, nameof(Cleanup)));
            
            sequence.AddJob(new ActionJob(TearDown, nameof(TearDown)));
            EditorCoroutineUtility.StartCoroutineOwnerless(sequence.Run());
        }

        DataContainer m_DataContainer;
        
        void ExecuteBlocking()
        {
            m_DataContainer = new DataContainer();
            
            var dependencyGraphProcessor = new NodeProcessor();
            var dependencyGraphRoot = new SampleNode("Dependency Graph Root");
            {
                dependencyGraphRoot.AddChild(new DefaultSystemSetupCreatorProcessor(m_DataContainer).Root);
                dependencyGraphRoot.AddChild(new ProcessingUnit(LoadSettingsFile));
                dependencyGraphRoot.AddChild(new DependencyGraphGeneratorProcessor(m_DataContainer).Root);
                // root.AddChild(new SampleNode("LoadDependencyGraph"));
                // root.AddChild(new SampleNode("RemoveScenesFromBuildProfile"));
            }
            dependencyGraphProcessor.SetRoot(dependencyGraphRoot);

            var progressBarTitle = "Dependency Graph 1/2";
            var progressBarInfo = "Processing Assets...";
            
            int progress = 0;
            int count = dependencyGraphProcessor.RemainingProcessCount;
            while (dependencyGraphProcessor.RemainingProcessCount > 0)
            {
                if (EditorUtility.DisplayCancelableProgressBar(progressBarTitle, progressBarInfo, (float)progress / count))
                    break;
                
                dependencyGraphProcessor.UpdateProcess();
                progress++;
            }

            EditorUtility.ClearProgressBar();
            
            //----------------------------------------------( Phase 2 )-------------------------------------------------
            
            var groupingGraphProcessor = new NodeProcessor();
            var groupingRoot = new SampleNode("Dependency Graph Root");
            {
                groupingRoot.AddChild(new IntakeFilterProcessor(m_DataContainer).Root);
                // groupingRoot.AddChild(new SampleNode("GenerateSubgraphs"));
                // groupingRoot.AddChild(new SampleNode("GenerateGroupLayout"));
                // groupingRoot.AddChild(new SampleNode("GenerateAddressableGroup"));
                // groupingRoot.AddChild(new SampleNode("AddAndSetupBootScene"));
                // groupingRoot.AddChild(new SampleNode("Cleanup"));
            }
            groupingGraphProcessor.SetRoot(groupingRoot);

            progressBarTitle = "Grouping 2/2";
            progressBarInfo = "Processing Assets...";
            
            progress = 0;
            count = groupingGraphProcessor.RemainingProcessCount;
            while (groupingGraphProcessor.RemainingProcessCount > 0)
            {
                if (EditorUtility.DisplayCancelableProgressBar(progressBarTitle, progressBarInfo, (float)progress / count))
                    break;
                
                groupingGraphProcessor.UpdateProcess();
                progress++;
            }

            EditorUtility.ClearProgressBar();
        }

        void Setup()
        {
        }

        void TearDown()
        {
        }
        
        void AddDefaultSettingsSequence()
        {
            m_DefaultSystemSetupCreator = new DefaultSystemSetupCreator(m_DependencyGraph, null, this);
            m_DefaultSystemSetupCreator.CreateDefaultSettingsFiles();
            Log(LogLevelID.Info, $"{nameof(AddDefaultSettingsSequence)} Completed");
        }

        IEnumerator GenerateDependencyGraph()
        {
            var dependencyGraphGenerator = new DependencyGraphGenerator();
            dependencyGraphGenerator.Start();
            yield return new WaitUntil(() => !dependencyGraphGenerator.InProgress);
            Log(LogLevelID.Info, $"{nameof(GenerateDependencyGraph)} Completed");
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
                    Log(LogLevelID.Info, $"{nameof(LoadDependencyGraph)} Completed");
                }));
        }
        
        IEnumerator RemoveScenesFromBuildProfile()
        {
            var preProcessing = new PreProcessingScenes(m_DependencyGraph, null);
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(preProcessing.Execute());
            Log(LogLevelID.Info, $"{nameof(RemoveScenesFromBuildProfile)} Completed");
        }
        
        IEnumerator GenerateIntakeFilter()
        {
            var preProcessingFilter = new PreProcessingFilter(m_DependencyGraph, null, this);
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(preProcessingFilter.SaveIgnoredAssetsToFile());
            Log(LogLevelID.Info, $"{nameof(GenerateIntakeFilter)} Completed");
        }

        IEnumerator GenerateSubgraphs()
        {
            var subgraphProcessor = new SubgraphProcessor(m_DependencyGraph, null);
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(subgraphProcessor.Execute());
            Log(LogLevelID.Info, $"{nameof(GenerateSubgraphs)} Completed");
        }

        IEnumerator GenerateGroupLayout()
        {
            var groupLayoutProcessor = new GroupLayoutProcessor(m_DependencyGraph, null, this);
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(groupLayoutProcessor.Execute());
            Log(LogLevelID.Info, $"{nameof(GenerateGroupLayout)} Completed");
        }

        IEnumerator GenerateAddressableGroup()
        {
            var processor = new AddressableGroupCreator(m_DependencyGraph, null);
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(processor.Execute());
            Log(LogLevelID.Info, $"{nameof(GenerateAddressableGroup)} Completed");
        }
        
        IEnumerator AddAndSetupBootScene()
        {
            var processor = new PostProcessScenes(m_DependencyGraph, null);
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(processor.Execute());
            Log(LogLevelID.Info, $"{nameof(AddAndSetupBootScene)} Completed");
        }
        
        IEnumerator Cleanup()
        {
            var processor = new PostProcessor(m_DependencyGraph, null);
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(processor.Execute());
            Log(LogLevelID.Info, $"{nameof(Cleanup)} Completed");
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

        void Log(LogLevelID logLevel, string message)
        {
            if (logLevel == LogLevelID.OnlyErrors)
            {
                Debug.LogError($"{GetType().Name}: {message}");
                return;
            }

            if (logLevel <= Settings.LogLevel)
            {
                Debug.Log($"{GetType().Name}: {message}");
            }
        }
    }
}