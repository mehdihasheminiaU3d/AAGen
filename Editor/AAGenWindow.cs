using System;
using System.Collections;
using System.Threading;
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
        
        void HeavyOperation(int arg)
        {
            Thread.Sleep(200);
            if (arg % 10 == 0)
                Debug.Log($"heavy operation {arg} completed");
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
                    EditorCoroutineUtility.StartCoroutineOwnerless(ExecuteNonBlocking2());
                    // ExecuteBlocking2();
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
        
        IEnumerator ExecuteNonBlocking2()
        {
            //1- Prepare main processes
            var processor1 = new CommandProcessor();
            processor1.Title = "processor1";
            for (int i = 0; i < 10; i++)
            {
                var arg = i;
                var processingUnit = new ProcessingUnit(() => HeavyOperation(arg));
                processingUnit.Info = $"Unit {i}";
                processor1.AddCommand(processingUnit);
            }
            processor1.EnqueueCommands();
            
            var processor2 = new CommandProcessor();
            processor2.Title = "processor2";
            for (int i = 0; i < 14; i++)
            {
                var arg = i;
                var processingUnit = new ProcessingUnit(() => HeavyOperation(arg));
                processingUnit.Info = $"Unit {i}";
                processor2.AddCommand(processingUnit);
            }
            processor2.EnqueueCommands();
            
            var processor3 = new CommandProcessor();
            processor3.Title = "processor3";
            for (int i = 0; i < 8; i++)
            {
                var arg = i;
                var processingUnit = new ProcessingUnit(() => HeavyOperation(arg));
                processingUnit.Info = $"Unit {i}";
                processor3.AddCommand(processingUnit);
            }
            processor3.EnqueueCommands();

            CommandProcessor[] allCommandProcessors = new[] { processor1, processor2, processor3 };

            
            //2- execution loop

            for (int i = 0; i < allCommandProcessors.Length; i++)
            {
                var currentProcessor = allCommandProcessors[i];

                float progressStart = (float)i / allCommandProcessors.Length;
                float progressEnd = (float)(i + 1) / allCommandProcessors.Length;

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
        }

        void ExecuteBlocking2()
        {
            //1- Prepare main processes
            var processor1 = new CommandProcessor();
            processor1.Title = "processor1";
            for (int i = 0; i < 10; i++)
            {
                var arg = i;
                var processingUnit = new ProcessingUnit(() => HeavyOperation(arg));
                processingUnit.Info = $"Unit {i}";
                processor1.AddCommand(processingUnit);
            }
            processor1.EnqueueCommands();
            
            var processor2 = new CommandProcessor();
            processor2.Title = "processor2";
            for (int i = 0; i < 14; i++)
            {
                var arg = i;
                var processingUnit = new ProcessingUnit(() => HeavyOperation(arg));
                processingUnit.Info = $"Unit {i}";
                processor2.AddCommand(processingUnit);
            }
            processor2.EnqueueCommands();
            
            var processor3 = new CommandProcessor();
            processor3.Title = "processor3";
            for (int i = 0; i < 8; i++)
            {
                var arg = i;
                var processingUnit = new ProcessingUnit(() => HeavyOperation(arg));
                processingUnit.Info = $"Unit {i}";
                processor3.AddCommand(processingUnit);
            }
            processor3.EnqueueCommands();

            CommandProcessor[] allCommandProcessors = new[] { processor1, processor2, processor3 };
            
            
            //2- execution loop
            try
            {
                for (int i = 0; i < allCommandProcessors.Length; i++)
                {
                    var currentProcessor = allCommandProcessors[i];

                    float progressStart = (float)i / allCommandProcessors.Length;
                    float progressEnd = (float)(i + 1) / allCommandProcessors.Length; 

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
                Debug.LogError(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
        
        void ExecuteBlocking()
        {
            m_DataContainer = new DataContainer();
            
            var dependencyGraphProcessor = new CommandProcessor();
            
            dependencyGraphProcessor.AddCommand(new DefaultSystemSetupCreatorProcessor(m_DataContainer).Root); 
            dependencyGraphProcessor.AddCommand(new ProcessingUnit(LoadSettingsFile));
            dependencyGraphProcessor.AddCommand(new DependencyGraphGeneratorProcessor(m_DataContainer).Root);
            // dependencyGraphProcessor.AddCommand(new SampleNode("LoadDependencyGraph"));
            // dependencyGraphProcessor.AddCommand(new SampleNode("RemoveScenesFromBuildProfile"));
            
            dependencyGraphProcessor.EnqueueCommands();

            var progressBarTitle = "Dependency Graph 1/2";
            var progressBarInfo = "Processing Assets...";
            
            int progress = 0;
            int count = dependencyGraphProcessor.RemainingCommandCount;

            try
            {
                while (dependencyGraphProcessor.RemainingCommandCount > 0)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(progressBarTitle, progressBarInfo,
                            (float)progress / count))
                        break;

                    dependencyGraphProcessor.ExecuteNextCommand();
                    progress++;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            
            //Do the same for all of them. All need their own progress
            
            //----------------------------------------------( Phase 2 )-------------------------------------------------
            // Even for creating the processing jobs and before doing any actual processing, we need the dependency graph.
            // So we have to do things in two phases. First preparations and generation of the dependency graph and then processing.
            
            var groupingGraphProcessor = new CommandProcessor();
            
            groupingGraphProcessor.AddCommand(new IntakeFilterProcessor(m_DataContainer).Root);
            groupingGraphProcessor.AddCommand(new SubgraphCommandProcessor(m_DataContainer).Root);
            groupingGraphProcessor.AddCommand(new GroupLayoutCommandProcessor(m_DataContainer).Root); //<-- No refinement for group layouts
            // groupingGraphProcessor.AddCommand(new SampleNode("GenerateAddressableGroup"));
            // groupingGraphProcessor.AddCommand(new SampleNode("AddAndSetupBootScene"));
            // groupingGraphProcessor.AddCommand(new SampleNode("Cleanup"));
            
            groupingGraphProcessor.EnqueueCommands();

            progressBarTitle = "Grouping 2/2";
            progressBarInfo = "Processing Assets...";
            
            progress = 0;
            count = groupingGraphProcessor.RemainingCommandCount;
            
            try
            {
                while (groupingGraphProcessor.RemainingCommandCount > 0)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(progressBarTitle, progressBarInfo, (float)progress / count))
                        break;
                
                    groupingGraphProcessor.ExecuteNextCommand();
                    progress++;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
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