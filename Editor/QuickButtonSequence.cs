using System.Collections;
using AAGen.Runtime;
using Unity.EditorCoroutines.Editor;
using UnityEngine;

namespace AAGen
{
    internal class QuickButtonSequence
    {
        AutomatedAssetGrouper m_ParentUi;
        AagenSettings m_Settings;
        
        DependencyGraph m_DependencyGraph;
        EditorJobGroup m_Sequence;

        bool m_LoadingInProgress = false;

        public QuickButtonSequence(AagenSettings settings,  AutomatedAssetGrouper parentUi)
        {
            m_ParentUi = parentUi;
            m_Settings = settings;
            m_Sequence = new EditorJobGroup(nameof(QuickButtonSequence));
        }

        public void Execute()
        {
            m_Sequence.AddJob(new ActionJob(Init, nameof(Init)));
            m_Sequence.AddJob(new CoroutineJob(GenerateDependencyGraph, nameof(GenerateDependencyGraph)));
            m_Sequence.AddJob(new CoroutineJob(LoadDependencyGraph, nameof(LoadDependencyGraph)));
            m_Sequence.AddJob(new ActionJob(AddDefaultSettingsSequence, nameof(AddDefaultSettingsSequence)));
            m_Sequence.AddJob(new CoroutineJob(PreProcessScenes, nameof(PreProcessScenes)));
            m_Sequence.AddJob(new CoroutineJob(PreProcess, nameof(PreProcess)));
            m_Sequence.AddJob(new CoroutineJob(Subgraphs, nameof(Subgraphs)));
            m_Sequence.AddJob(new CoroutineJob(GroupLayout, nameof(GroupLayout)));
            m_Sequence.AddJob(new CoroutineJob(AddressableGroup, nameof(AddressableGroup)));
            m_Sequence.AddJob(new CoroutineJob(PostProcessScenes, nameof(PostProcessScenes)));
            m_Sequence.AddJob(new CoroutineJob(PostProcess, nameof(PostProcess)));
            EditorCoroutineUtility.StartCoroutineOwnerless(m_Sequence.Run());
        }
        
        void Init()
        {
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

            EditorCoroutineUtility.StartCoroutineOwnerless(DependencyGraphUtil.LoadFromFileAsync<DependencyGraph>(
                filePath,
                (dependencyGraph) =>
                {
                    m_DependencyGraph = dependencyGraph;
                    m_LoadingInProgress = false;
                }));
        }

        void AddDefaultSettingsSequence()
        {
            var processor = new DefaultSystemSetupCreator(m_DependencyGraph, null, m_ParentUi);
            processor.CreateDefaultSettingsFiles();
            m_Sequence.AddJob(processor._sequence);
        }
        
        IEnumerator PreProcessScenes()
        {
            var preProcessing = new PreProcessingScenes(m_DependencyGraph, null);
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(preProcessing.Execute());
        }


        IEnumerator PreProcess()
        {
            var preProcessingFilter = new PreProcessingFilter(m_DependencyGraph, null, m_ParentUi);
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(preProcessingFilter.SaveIgnoredAssetsToFile());
        }

        IEnumerator Subgraphs()
        {
            var subgraphProcessor = new SubgraphProcessor(m_DependencyGraph, null);
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(subgraphProcessor.Execute());
        }

        IEnumerator GroupLayout()
        {
            var groupLayoutProcessor = new GroupLayoutProcessor(m_DependencyGraph, null, m_ParentUi);
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
    }
}