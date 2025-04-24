using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using AAGen.AssetDependencies;
using AAGen.Shared;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace AAGen
{
    internal class AAGenWindow : EditorWindow 
    {
        [MenuItem("Tools/AAGen", priority = 100)] //<-- ToDo: Add constants here 
        public static void ShowWindow()
        {
            var window = GetWindow<AAGenWindow>("AAGen");
            window.minSize = new Vector2(400, 200); 
        }

        AagenSettings m_Settings;
        EditorPersistentValue<string> _settingsAssetPath = new (null, "EPK_AAG_SettingsPath");
        
        const string k_BoxStyleName = "Box";
        const float k_Space = 5f;
        const string k_QuickButtonLabel = "Generate Addressable Groups Automatically";
        const int k_QuickButtonWidth = 300;
        const int k_QuickButtonHeight = 30;
        
        DependencyGraph m_DependencyGraph;
        DefaultSystemSetupCreator m_DefaultSystemSetupCreator;

        bool m_LoadingInProgress = false;

        bool m_IsProcessing = false;
        DataContainer m_DataContainer;
        
        void OnEnable()
        {
            var assetPath = _settingsAssetPath.Value;
            if (!string.IsNullOrEmpty(assetPath))
            {
                m_Settings = AssetDatabase.LoadAssetAtPath<AagenSettings>(assetPath);
            }
        }

        void OnDisable()
        {
            if (m_Settings != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(m_Settings);
                _settingsAssetPath.Value = assetPath;
            }
            else
            {
                _settingsAssetPath.ClearPersistentData();
            }
        }

        void LoadSettingsFile()
        {
            m_Settings = AssetDatabase.LoadAssetAtPath<AagenSettings>(m_DataContainer.SettingsFilePath);
        }
        
        void HeavyOperation(int arg)
        {
            Thread.Sleep(200);
            if (arg % 10 == 0)
                Debug.Log($"heavy operation {arg} completed");
        }

        void OnGUI()
        {
            GUILayout.BeginVertical(k_BoxStyleName);
            GUILayout.Space(k_Space);

            DrawCentered(() =>
            {
                m_Settings = (AagenSettings)EditorGUILayout.ObjectField(m_Settings, typeof(AagenSettings), false,
                    GUILayout.Width(k_QuickButtonWidth));
            }, k_QuickButtonWidth);

            GUILayout.Space(k_Space);

            DrawCentered(() =>
            {
                if (GUILayout.Button(k_QuickButtonLabel, GUILayout.MinWidth(k_QuickButtonWidth), GUILayout.Height(k_QuickButtonHeight)))
                {
                    if (m_IsProcessing)
                        return;

                    if (m_Settings != null && m_Settings.RunInBackground)
                        EditorCoroutineUtility.StartCoroutineOwnerless(RunAsyncLoop());
                    else
                        RunBlockingLoop();
                }
            }, k_QuickButtonWidth);

            GUILayout.Space(k_Space);
            GUILayout.EndVertical();
        }
        
        void InitializeDataContainer()
        {
            m_DataContainer = new DataContainer
            {
                Settings = m_Settings,
                SettingsFilePath = AssetDatabase.GetAssetPath(m_Settings)
            };
        }
        
        List<CommandQueue> InitializeCommands()
        {
            var processor1 = new CommandQueue();
            processor1.Title = "processor1";
            for (int i = 0; i < 10; i++)
            {
                var arg = i;
                var processingUnit = new ActionCommand(() => HeavyOperation(arg));
                processingUnit.Info = $"Unit {i}";
                processor1.AddCommand(processingUnit);
            }
            processor1.EnqueueCommands();
            
            var loadSettingsQueue = new CommandQueue();
            var loadSettingsCommand = new ActionCommand(LoadSettingsFile, nameof(LoadSettingsFile));
            loadSettingsQueue.AddCommand(loadSettingsCommand);
            loadSettingsQueue.EnqueueCommands();

            var commandQueues =  new List<CommandQueue>  
            { 
                processor1,
                new SettingsFilesCommandQueue(m_DataContainer),
                loadSettingsQueue,
            };

            if (m_Settings == null || m_Settings.ProcessingSteps.HasFlag(ProcessingStepID.GenerateDependencyGraph))
                commandQueues.Add(new DependencyGraphCommandQueue(m_DataContainer));
            
            return commandQueues;
        }
        
        IEnumerator RunAsyncLoop()
        {
            m_IsProcessing = true;
            
            InitializeDataContainer();
            var commandQueues = InitializeCommands();

            for (int i = 0; i < commandQueues.Count; i++)
            {
                var currentProcessor = commandQueues[i];
                currentProcessor.PreExecute();

                float progressStart = (float)i / commandQueues.Count;
                float progressEnd = (float)(i + 1) / commandQueues.Count;

                int progress = 0;
                int totalCount = currentProcessor.RemainingCommandCount;

                var progressBarTitle = currentProcessor.Title;
                var progressBarInfo = "Processing ...";
                
                var progressId = Progress.Start(progressBarTitle);

                while (currentProcessor.RemainingCommandCount > 0)
                {
                    var info = string.Empty;
                    bool error = false;
                    Exception exception = null;
                    
                    try
                    {
                        info = currentProcessor.ExecuteNextCommand();
                    }
                    catch (Exception e)
                    {
                        error = true;
                        exception = e;
                    }

                    if (error)
                    {
                        Debug.LogError(exception.Message);
                        Progress.Remove(progressId);
                        m_IsProcessing = false;
                        yield break;
                    }
                    
                    progress++;
                    progressBarInfo = string.IsNullOrEmpty(info) ? progressBarInfo : info;

                    var percentage = progressStart + ((float)progress / totalCount) * (progressEnd - progressStart);
                    Progress.Report(progressId, percentage, progressBarInfo);
                    yield return null;
                }
                
                Progress.Remove(progressId);
            }
            
            m_IsProcessing = false;
        }
        
        void RunBlockingLoop()
        {
            m_IsProcessing = true;
            
            InitializeDataContainer();
            var commandQueues = InitializeCommands();
            
            try
            {
                for (int i = 0; i < commandQueues.Count; i++)
                {
                    var currentProcessor = commandQueues[i];
                    currentProcessor.PreExecute();

                    float progressStart = (float)i / commandQueues.Count;
                    float progressEnd = (float)(i + 1) / commandQueues.Count; 

                    int progress = 0;
                    int totalCount = currentProcessor.RemainingCommandCount;
                    
                    var progressBarTitle = currentProcessor.Title;
                    var progressBarInfo = "Processing ...";
                    
                    while (currentProcessor.RemainingCommandCount > 0)
                    {
                        var info = currentProcessor.ExecuteNextCommand();
                        progress++;
                        
                        progressBarInfo = string.IsNullOrEmpty(info) ? progressBarInfo : info;
                        
                        if (EditorUtility.DisplayCancelableProgressBar(progressBarTitle, progressBarInfo,
                                progressStart + ((float)progress / totalCount) * (progressEnd - progressStart))) 
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                m_IsProcessing = false;
            }
        }
        
        // <--- Log flags
        //
        // void AddDefaultSettingsSequence()
        // {
        //     m_DefaultSystemSetupCreator = new DefaultSystemSetupCreator(m_DependencyGraph, null, this);
        //     m_DefaultSystemSetupCreator.CreateDefaultSettingsFiles();
        //     Log(LogLevelID.Info, $"{nameof(AddDefaultSettingsSequence)} Completed");
        // }
        //
        // IEnumerator GenerateDependencyGraph()
        // {
        //     var dependencyGraphGenerator = new DependencyGraphGenerator();
        //     dependencyGraphGenerator.Start();
        //     yield return new WaitUntil(() => !dependencyGraphGenerator.InProgress);
        //     Log(LogLevelID.Info, $"{nameof(GenerateDependencyGraph)} Completed");
        // }
        //
        // IEnumerator LoadDependencyGraph()
        // {
        //     if (m_LoadingInProgress)
        //         yield break;
        //
        //     string filePath = Constants.DependencyGraphFilePath;
        //     m_LoadingInProgress = true;
        //
        //     EditorCoroutineUtility.StartCoroutineOwnerless(FileUtils.LoadFromFileAsync<DependencyGraph>(
        //         filePath,
        //         (dependencyGraph) =>
        //         {
        //             m_DependencyGraph = dependencyGraph;
        //             m_LoadingInProgress = false;
        //             Log(LogLevelID.Info, $"{nameof(LoadDependencyGraph)} Completed");
        //         }));
        // }
        //
        // IEnumerator RemoveScenesFromBuildProfile()
        // {
        //     var preProcessing = new PreProcessingScenes(m_DependencyGraph, null);
        //     yield return EditorCoroutineUtility.StartCoroutineOwnerless(preProcessing.Execute());
        //     Log(LogLevelID.Info, $"{nameof(RemoveScenesFromBuildProfile)} Completed");
        // }
        //
        // IEnumerator GenerateIntakeFilter()
        // {
        //     var preProcessingFilter = new PreProcessingFilter(m_DependencyGraph, null, this);
        //     yield return EditorCoroutineUtility.StartCoroutineOwnerless(preProcessingFilter.SaveIgnoredAssetsToFile());
        //     Log(LogLevelID.Info, $"{nameof(GenerateIntakeFilter)} Completed");
        // }
        //
        // IEnumerator GenerateSubgraphs()
        // {
        //     var subgraphProcessor = new SubgraphProcessor(m_DependencyGraph, null);
        //     yield return EditorCoroutineUtility.StartCoroutineOwnerless(subgraphProcessor.Execute());
        //     Log(LogLevelID.Info, $"{nameof(GenerateSubgraphs)} Completed");
        // }
        //
        // IEnumerator GenerateGroupLayout()
        // {
        //     var groupLayoutProcessor = new GroupLayoutProcessor(m_DependencyGraph, null, this);
        //     yield return EditorCoroutineUtility.StartCoroutineOwnerless(groupLayoutProcessor.Execute());
        //     Log(LogLevelID.Info, $"{nameof(GenerateGroupLayout)} Completed");
        // }
        //
        // IEnumerator GenerateAddressableGroup()
        // {
        //     var processor = new AddressableGroupCreator(m_DependencyGraph, null);
        //     yield return EditorCoroutineUtility.StartCoroutineOwnerless(processor.Execute());
        //     Log(LogLevelID.Info, $"{nameof(GenerateAddressableGroup)} Completed");
        // }
        //
        // IEnumerator AddAndSetupBootScene()
        // {
        //     var processor = new PostProcessScenes(m_DependencyGraph, null);
        //     yield return EditorCoroutineUtility.StartCoroutineOwnerless(processor.Execute());
        //     Log(LogLevelID.Info, $"{nameof(AddAndSetupBootScene)} Completed");
        // }
        //
        // IEnumerator Cleanup()
        // {
        //     var processor = new PostProcessor(m_DependencyGraph, null);
        //     yield return EditorCoroutineUtility.StartCoroutineOwnerless(processor.Execute());
        //     Log(LogLevelID.Info, $"{nameof(Cleanup)} Completed");
        // }
        
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

            if (logLevel <= m_Settings.LogLevel)
            {
                Debug.Log($"{GetType().Name}: {message}");
            }
        }
    }
}