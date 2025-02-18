using System.Collections;
using System.Collections.Generic;
using AAGen.AssetDependencies;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using AAGen.Shared;

namespace AAGen
{
    internal class PreProcessingScenes : DependencyGraphProcessor 
    {
        public PreProcessingScenes(DependencyGraph dependencyGraph, EditorUiGroup uiGroup) : base(dependencyGraph, uiGroup)
        {
        }
        
        EditorJobGroup m_Sequence;
        List<string> m_ScenesInBuild;
        
        public IEnumerator Execute()
        {
            m_Sequence = new EditorJobGroup(nameof(PreProcessingScenes));
            m_Sequence.AddJob(new ActionJob(Init, nameof(Init)));
            m_Sequence.AddJob(new ActionJob(RecordScenesInBuild, nameof(RecordScenesInBuild)));
            m_Sequence.AddJob(new CoroutineJob(SaveToFile, nameof(SaveToFile)));
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(m_Sequence.Run());
        }

        void RecordScenesInBuild()
        {
            m_ScenesInBuild = new List<string>();
            
            foreach (var scene in EditorBuildSettings.scenes) //Record in the same order
            {
                //ToDo: Do we need to record all? We'll only use the scene at index = 0
                m_ScenesInBuild.Add(scene.path);
            }

            //Remove all from build settings
            // ToDo: Should we skip the scenes that are disabled and not included in the build? 
            EditorBuildSettings.scenes = null;
        }

        IEnumerator SaveToFile()
        {
            var filePath = Constants.FilePaths.ScenePreprocess;
            yield return FileUtils.SaveToFileAsync(m_ScenesInBuild, filePath, (success) =>
            {
                if(!success)
                    Debug.LogError($"Failed to save {filePath}!");
            });
        }
    }
}