using System.Collections;
using AAGen.Runtime;
using Unity.EditorCoroutines.Editor;
using UnityEngine;

namespace AAGen.Editor.DependencyGraph
{
    internal class QuickButtonSequence
    {
        AutomatedAssetGrouper m_ParentUi;
        AagSettings m_Settings;
        
        DependencyGraph m_DependencyGraph;
        EditorJobGroup m_Sequence;

        bool m_LoadingInProgress = false;

        public QuickButtonSequence(AagSettings settings,  AutomatedAssetGrouper parentUi)
        {
            m_ParentUi = parentUi;
            m_Settings = settings;
            m_Sequence = new EditorJobGroup(nameof(QuickButtonSequence));
        }

        public void Execute()
        {
            
            m_Sequence.AddJob(new ActionJob(Init, nameof(Init)));
            m_Sequence.AddJob(new CoroutineJob(LoadDependencyGraph, nameof(LoadDependencyGraph)));
            m_Sequence.AddJob(new ActionJob(AddDefaultSettingsSequence, nameof(AddDefaultSettingsSequence)));
            EditorCoroutineUtility.StartCoroutineOwnerless(m_Sequence.Run());
        }
        
        void Init()
        {
            
        }

        IEnumerator LoadDependencyGraph()
        {
            if (m_LoadingInProgress)
                yield break;

            string filePath = DependencyGraphConstants.DependencyGraphFilePath;
            m_LoadingInProgress = true;

            EditorCoroutineUtility.StartCoroutineOwnerless(DependencyGraphUtil.LoadFromFileAsync<DependencyGraph>(
                filePath,
                (dependencyGraph) =>
                {
                    m_DependencyGraph = dependencyGraph;
                    m_LoadingInProgress = false;
                    Debug.Log("load dependency graph completed");
                }));
        }
        
        void AddDefaultSettingsSequence()
        {
            if(m_Settings == null)
            {
                var processor = new DefaultSystemSetupCreator(m_DependencyGraph, null, m_ParentUi);
                processor.CreateDefaultSettingsFiles();
                m_Sequence.AddJob(processor._sequence);
            }
        }
    }
}