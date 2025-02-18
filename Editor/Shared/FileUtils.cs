using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AAGen.Shared
{
    internal static class FileUtils
    {
        public static void SaveToFile(string filePath, string data)
        {
            EnsureDirectoryExist(filePath);
            
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] binaryData = Encoding.UTF8.GetBytes(data);
                fileStream.Write(binaryData, 0, binaryData.Length);
            }
        }

        public static string LoadFromFile(string path)
        {
            string data = string.Empty;

            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                byte[] bytes = new byte[fileStream.Length];
                fileStream.Read(bytes, 0, bytes.Length);
                data = Encoding.UTF8.GetString(bytes);
            }
            
            return data;
        }
        
        public static IEnumerator SaveToFileAsync<T>(T obj, string filePath, Action<bool> onComplete = null, int bytesPerStep = 1024 * 1024)
        {
            EnsureDirectoryExist(filePath);
            
            var streamWriter = new StreamWriter(filePath, false, Encoding.UTF8);
            var data = new StringBuilder();

            try
            {
                data.Append(JsonConvert.SerializeObject(obj, Formatting.Indented)); //Formatting.None
            }
            catch (Exception ex)
            {
                Debug.LogError("Error Serializing JSON: " + ex.Message);
                streamWriter.Close();
                onComplete?.Invoke(false);
                yield break;
            }
            
            while (true)
            {
                try
                {
                    if (data.Length > 0)
                    {
                        int lengthToWrite = Math.Min(bytesPerStep, data.Length);
                        char[] buffer = new char[lengthToWrite];
                        data.CopyTo(0, buffer, 0, lengthToWrite);

                        streamWriter.Write(buffer, 0, lengthToWrite);
                        data.Remove(0, lengthToWrite);
                    }
                    else
                    {
                        streamWriter.Close();
                        break;
                    }
                }
                catch (JsonException ex)
                {
                    Debug.LogError($"{nameof(SaveToFileAsync)} Error : {ex.Message}");
                    streamWriter.Close();
                    onComplete?.Invoke(false);
                    yield break;
                }

                yield return null;
            }
            
            onComplete?.Invoke(true);
        }
        
        public static IEnumerator LoadFromFileAsync<T>(string filePath, Action<T> onComplete, int bytesPerStep = 1024 * 1024)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError("File not found: " + filePath);
                yield break;
            }
            
            StreamReader streamReader = new StreamReader(filePath);
            StringBuilder data = new StringBuilder();

            while (true)
            {
                try
                {
                    char[] buffer = new char[bytesPerStep];
                    int bytesRead = streamReader.Read(buffer, 0, bytesPerStep);

                    if (bytesRead > 0)
                    {
                        data.Append(buffer, 0, bytesRead);
                    }
                    else
                    {
                        streamReader.Close();
                        onComplete?.Invoke(JsonConvert.DeserializeObject<T>(data.ToString()));
                        break;
                    }

                }
                catch (JsonException ex)
                {
                    Debug.LogError($"{nameof(LoadFromFileAsync)} Error : {ex.Message}");
                    streamReader.Close();
                    break;
                }

                yield return null;
            }
        }

        public static bool ShouldIgnoreAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || 
                string.IsNullOrWhiteSpace(assetPath) ||
                AssetDatabase.IsValidFolder(assetPath))
                return true;
            
            //Ignore code-only assets
            var extension = Path.GetExtension(assetPath);
            if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".hlsl ", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".asmdef", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".asmref", StringComparison.OrdinalIgnoreCase))
                return true; 
            
            if (ProjectSettingsProvider.DebugMode &&
                !assetPath.Contains(ProjectSettingsProvider.DebugRootFolder, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        public static void EnsureDirectoryExist(string filePath) //ToDo: Add to a shared util Class
        {
            string directoryPath = Path.GetDirectoryName(filePath);
            
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
        
        public static IEnumerator SaveToFileAsync<T>(string serializedData, string filePath, Action<bool> onComplete = null, int bytesPerStep = 1024 * 1024)
        {
            EnsureDirectoryExist(filePath);
            
            var streamWriter = new StreamWriter(filePath, false, Encoding.UTF8);
            
            while (true)
            {
                try
                {
                    if (serializedData.Length > 0)
                    {
                        int lengthToWrite = Math.Min(bytesPerStep, serializedData.Length);
                        char[] buffer = new char[lengthToWrite];
                        serializedData.CopyTo(0, buffer, 0, lengthToWrite);

                        streamWriter.Write(buffer, 0, lengthToWrite);
                        serializedData.Remove(0, lengthToWrite);
                    }
                    else
                    {
                        streamWriter.Close();
                        break;
                    }
                }
                catch (JsonException ex)
                {
                    Debug.LogError($"{nameof(SaveToFileAsync)} Error : {ex.Message}");
                    streamWriter.Close();
                    onComplete?.Invoke(false);
                    yield break;
                }

                yield return null;
            }
            
            onComplete?.Invoke(true);
        }
    }
}
