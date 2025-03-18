using UnityEditor;
using UnityEngine;

namespace AAGen
{
    internal static class DefaultSettingsFileGenerator
    {
        [MenuItem("Tools/Generate Default settings")]
        private static void SaveTextAsset()
        {
            var path = EditorUtility.SaveFilePanel("Save Text Asset", 
                "Assets", 
                "NewAagenSettings", 
                "asset");

            if (string.IsNullOrEmpty(path))
                return;

            if (!path.StartsWith(Application.dataPath))
            {
                Debug.LogError("Invalid path. The file must be saved inside the Assets folder.");
                return;
            }

            string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
        
            var defaultSystemSetupCreator = new DefaultSystemSetupCreator(null, null, null);
            defaultSystemSetupCreator.CreateDefaultSettingsFileAtPath(relativePath);

            Debug.Log($"asset saved at: {relativePath}");
        }
    }
}