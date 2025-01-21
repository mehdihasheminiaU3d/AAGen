using System.Collections;
using System.Collections.Generic;
using AAGen.Runtime;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace AAGen
{
    internal class PostProcessScenes : DependencyGraphProcessor
    {
        public PostProcessScenes(DependencyGraph dependencyGraph, EditorUiGroup uiGroup) : base(dependencyGraph, uiGroup)
        {
        }
       
        EditorJobGroup m_Sequence;
        string m_OriginalBootScene;

        public IEnumerator Execute()
        {
            m_Sequence = new EditorJobGroup(nameof(AddressableGroupCreator));
            m_Sequence.AddJob(new ActionJob(Init, nameof(Init)));
            m_Sequence.AddJob(new CoroutineJob(FindOriginalBootScene, nameof(FindOriginalBootScene)));
            m_Sequence.AddJob(new ActionJob(CreateNewBootScene, nameof(CreateNewBootScene)));
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(m_Sequence.Run());
        }

        IEnumerator FindOriginalBootScene()
        {
            string scenePreprocessFilePath = Constants.FilePaths.ScenePreprocess;

            yield return DependencyGraphUtil.LoadFromFileAsync<List<string>>(scenePreprocessFilePath,
                (data) =>
                {
                    if (data is { Count: > 0 })
                    {
                        m_OriginalBootScene = data[0];
                    }
                });
        }

        void CreateNewBootScene()
        { 
            // Step 1: Create a new empty scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Step 2: Add a GameObject to the scene
            GameObject newGameObject = new GameObject("Boot Loader");
            var bootLoader = newGameObject.AddComponent<BootLoader>();
            AssignBootLoaderValue(bootLoader, m_OriginalBootScene);
                
            // Step 3: Make sure the GameObject is part of the new scene
            SceneManager.MoveGameObjectToScene(newGameObject, newScene); 
            
            var filePath = Constants.FilePaths.DefaultBootScene;
            
            DependencyGraphUtil.EnsureDirectoryExist(filePath);

            // Step 4: Save the scene to the chosen path
            bool saveResult = EditorSceneManager.SaveScene(newScene, filePath);
            if (saveResult)
            {
                Debug.Log($"Scene saved successfully at: {filePath}");
                AddSceneToBuildSettings(newScene.path);
            }
            else
            {
                Debug.LogError("Failed to save the scene.");
            }
        }

        void AddSceneToBuildSettings(string scenePath)
        {
            // Check if the scene already exists in the Build Settings
            EditorBuildSettingsScene[] currentScenes = EditorBuildSettings.scenes;
            foreach (EditorBuildSettingsScene scene in currentScenes)
            {
                if (scene.path == scenePath)
                {
                    return;
                }
            }

            // Add new scene to Build Settings
            EditorBuildSettingsScene newScene = new EditorBuildSettingsScene(scenePath, true);
            var updatedScenes = new EditorBuildSettingsScene[currentScenes.Length + 1];
            currentScenes.CopyTo(updatedScenes, 1);
            updatedScenes[0] = newScene;

            EditorBuildSettings.scenes = updatedScenes; // Update the Build Settings
            Debug.Log($"Scene added to Build Settings: {scenePath}");
        }

        void AssignBootLoaderValue(BootLoader bootLoader, string scenePath)
        {
            if (bootLoader == null)
            {
                Debug.LogError($"Boot loader component is null");
                return;
            }

            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogWarning($"original boot scene path not found!");
                return;
            }
            
            var sceneEntry = GetAddressableEntryByPath(scenePath);
            
            AssetReference sceneReference = new AssetReference(sceneEntry.guid);
            bootLoader.m_SceneToLoad = sceneReference; // Set the AssetReference
        }
        
        static AddressableAssetEntry GetAddressableEntryByPath(string assetPath) //ToDo: Add to a shared util Class
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("Asset path is null or empty.");
                return null;
            }

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressable Asset Settings not found!");
                return null;
            }

            // Find the Addressable Asset Entry by its path
            AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(assetPath));

            if (entry != null)
            {
                Debug.Log($"Found Addressable Entry: {entry.address} in group {entry.parentGroup.Name}");
                return entry;
            }
            else
            {
                Debug.LogError($"No Addressable Entry found for asset path: {assetPath}");
                return null;
            }
        }
    }
}