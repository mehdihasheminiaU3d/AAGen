using AAGen.Shared;
using UnityEditor;
using UnityEngine;

namespace AAGen
{
    internal static class DefaultSettingsFileGenerator
    {
        [MenuItem(Constants.Menus.Root + "Generate Settings File", priority = Constants.Menus.AAGenMenuPriority)] 
        static void OpenSaveSettingsPanel()
        {
            var path = EditorUtility.SaveFilePanel("Generate Settings Asset at", 
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

            SettingsFilesCommandQueue.CreateAddressableSettingsIfRequired();
            SettingsFilesCommandQueue.CreateDefaultToolSettingsAtPath(relativePath);

            Debug.Log($"asset saved at: {relativePath}");
        }
    }
}