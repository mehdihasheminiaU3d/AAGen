using System;
using System.Collections;
using System.Collections.Generic;
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
            var loadSettingsQueue = new CommandQueue();
            var loadSettingsCommand = new ActionCommand(LoadSettingsFile, nameof(LoadSettingsFile));
            loadSettingsQueue.AddCommand(loadSettingsCommand);
            loadSettingsQueue.EnqueueCommands();

            var commandQueues =  new List<CommandQueue>  
            { 
                new SettingsFilesCommandQueue(m_DataContainer),
                loadSettingsQueue,
            };

            if (m_Settings == null || m_Settings.ProcessingSteps.HasFlag(ProcessingStepID.GenerateDependencyGraph))
                commandQueues.Add(new DependencyGraphCommandQueue(m_DataContainer));
            
            if (m_Settings == null || m_Settings.ProcessingSteps.HasFlag(ProcessingStepID.AssetIntakeFilter))
                commandQueues.Add(new IntakeFilterCommandQueue(m_DataContainer));
            
            if (m_Settings == null || m_Settings.ProcessingSteps.HasFlag(ProcessingStepID.GenerateSubGraphs))
                commandQueues.Add(new SubgraphCommandQueue(m_DataContainer));
            
            if (m_Settings == null || m_Settings.ProcessingSteps.HasFlag(ProcessingStepID.GenerateGroupLayout))
                commandQueues.Add(new GroupLayoutCommandQueue(m_DataContainer));
            
            if (m_Settings == null || m_Settings.ProcessingSteps.HasFlag(ProcessingStepID.GenerateAddressableGroups))
                commandQueues.Add(new AddressableGroupCommandQueue(m_DataContainer));
            
            if (m_Settings == null || m_Settings.ProcessingSteps.HasFlag(ProcessingStepID.Cleanup))
                commandQueues.Add(new AddressableCleanupCommandQueue(m_DataContainer));
            
            return commandQueues;
            
            //     Log(LogLevelID.Info, $"{nameof(AddDefaultSettingsSequence)} Completed");
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
                        m_IsProcessing = false;
                        StopAssetEditingIfNeeded();
                        Progress.Remove(progressId);
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
                StopAssetEditingIfNeeded();
            }
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

            if (logLevel <= m_Settings.LogLevel)
            {
                Debug.Log($"{GetType().Name}: {message}");
            }
        }

        void StopAssetEditingIfNeeded()
        {
            if(m_DataContainer.AssetEditingInProgress)
            {
                AssetDatabase.StopAssetEditing();
                m_DataContainer.AssetEditingInProgress = false;
            }
        }
    }
}