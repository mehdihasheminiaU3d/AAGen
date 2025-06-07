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
        #region Static Methods
        /// <summary>
        /// Write string data to a file in a blocking fashion.
        /// </summary>
        /// <param name="filePath">The file path to write to.</param>
        /// <param name="data">The string data.</param>
        public static void SaveToFile(string filePath, string data)
        {
            // Ensure that the directories in the file path exist.
            EnsureDirectoryExist(filePath);
            
            // Open a file stream at the file path for writing to the file.
            // It will create a new file or overwrite a previously created file.
            // Will block any concurrent access to the same file.
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                // Get a byte array representation of the string data (to avoid any interpretaion of the string)
                byte[] binaryData = Encoding.UTF8.GetBytes(data);

                // Write all the data to the file.
                fileStream.Write(binaryData, 0, binaryData.Length);
            }
        }

        /// <summary>
        /// Read the content of the file from a file in a blocking fashion.
        /// </summary>
        /// <param name="filePath">The file path to write to.</param>
        public static string LoadFromFile(string filePath)
        {
            // Default the data to an empty string.
            string data = string.Empty;

            // Open a file stream at the file path for reading from the file.
            // It will open the file stream only if the file exists.
            // Will block any concurrent access to the same file.
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                /// Create a byte array buffer that is the length of the contents of the file.
                byte[] bytes = new byte[fileStream.Length];

                // Write the contents of the file into the byte array buffer.
                fileStream.Read(bytes, 0, bytes.Length);

                // Convert the byte array buffer into a UTF8 string array.
                data = Encoding.UTF8.GetString(bytes);
            }
            
            // Return the result.
            return data;
        }

        /// <summary>
        /// Write string data to a file in a non-blocking fashion.
        /// </summary>
        /// <typeparam name="T">The type of the object to write to file.</typeparam>
        /// <param name="obj">The instance of the object to write to file.</param>
        /// <param name="filePath">The file path to write to.</param>
        /// <param name="onComplete">A function object to handle when the operation completes.</param>
        /// <param name="bytesPerStep">The number of bytes to process during a time slice.</param>
        /// <returns>A sequence of incremental sub-routines that the entire task is comprised of.</returns>
        public static IEnumerator SaveToFileAsync<T>(T obj, string filePath, Action<bool> onComplete = null, int bytesPerStep = 1024 * 1024)
        {
            // Ensure that the directories in the file path exist.
            EnsureDirectoryExist(filePath);
            
            // Create a stream writer.
            // NOTE: do we need to make this at this point or can it wait for after the while(true)?
            var streamWriter = new StreamWriter(filePath, false, Encoding.UTF8);

            // Create a string builder.
            var data = new StringBuilder();

            try
            {

                // Serialize the object into an indented JSON formatted string value.
                string jsonValue = JsonConvert.SerializeObject(obj, Formatting.Indented); //Formatting.None

                // Append it the formatted string to the builder.
                data.Append(jsonValue);
            }
            catch (Exception ex)
            {
                // If there were any exceptions thrown:

                // Log the exception.
                Debug.LogError("Error Serializing JSON: " + ex.Message);

                // Close the file that is open for writing.
                streamWriter.Close();

                // Notify the subscribers that the operation is complete.
                onComplete?.Invoke(false);

                // Do nothing else.
                yield break;
            }
            
            // Iterate until a condition terminates the loop.
            while (true)
            {
                try
                {
                    // If there are elements in the string builder, then:
                    if (data.Length > 0)
                    {
                        // Get the number of bytes to write at a time.
                        // Use the smaller of the two: the bytes remaining, or the chunk size.
                        int lengthToWrite = Math.Min(bytesPerStep, data.Length);

                        // Create a buffer transfer the file data.
                        char[] buffer = new char[lengthToWrite];

                        // Copy the data in the string builder to the char buffer.
                        data.CopyTo(0, buffer, 0, lengthToWrite);

                        // Write the buffer data to the stream writer.
                        streamWriter.Write(buffer, 0, lengthToWrite);

                        // Clear the items from the builder.
                        data.Remove(0, lengthToWrite);
                    }
                    else
                    {
                        // Otherwise, there are no elements in the string builder.

                        // Close the file that is open for writing.
                        streamWriter.Close();

                        // Break out of the while loop.
                        break;
                    }
                }
                catch (JsonException ex)
                {
                    // If there were any exceptions thrown:

                    // Log the exception.
                    Debug.LogError($"{nameof(SaveToFileAsync)} Error : {ex.Message}");
                    
                    // Close the file that is open for writing.
                    streamWriter.Close();

                    // Notify the subscribers that the operation is complete.
                    onComplete?.Invoke(false);

                    // Do nothing else.
                    yield break;
                }

                // Complete this time slice of the operation.
                yield return null;
            }

            // Notify the subscribers that the operation is complete.
            onComplete?.Invoke(true);
        }

        /// <summary>
        /// Write string data to a file in a non-blocking fashion.
        /// </summary>
        /// <param name="serializedData">The instance of the object to write to file.</param>
        /// <param name="filePath">The file path to write to.</param>
        /// <param name="onComplete">A function object to handle when the operation completes.</param>
        /// <param name="bytesPerStep">The number of bytes to process during a time slice.</param>
        /// <returns>A sequence of incremental sub-routines that the entire task is comprised of.</returns>
        public static IEnumerator SaveToFileAsync<T>(string serializedData, string filePath, Action<bool> onComplete = null, int bytesPerStep = 1024 * 1024)
        {
            // Ensure that the directories in the file path exist.
            EnsureDirectoryExist(filePath);

            // Create a stream writer.
            // NOTE: do we need to make this at this point or can it wait for after the while(true)?
            var streamWriter = new StreamWriter(filePath, false, Encoding.UTF8);

            // Iterate until a condition terminates the loop.
            while (true)
            {
                try
                {
                    // If there are elements in the string, then:
                    if (serializedData.Length > 0)
                    {
                        // Get the number of bytes to write at a time.
                        // Use the smaller of the two: the bytes remaining, or the chunk size.
                        int lengthToWrite = Math.Min(bytesPerStep, serializedData.Length);
                        // Create a buffer transfer the file data.
                        char[] buffer = new char[lengthToWrite];

                        // Copy the data in the string builder to the char buffer.
                        serializedData.CopyTo(0, buffer, 0, lengthToWrite);

                        // Write the buffer data to the stream writer.
                        streamWriter.Write(buffer, 0, lengthToWrite);

                        // Clear the items from the builder.
                        serializedData.Remove(0, lengthToWrite);
                    }
                    else
                    {
                        // Otherwise, there are no elements in the string.

                        // Close the file that is open for writing.
                        streamWriter.Close();

                        // Break out of the while loop.
                        break;
                    }
                }
                catch (JsonException ex)
                {
                    // If there were any exceptions thrown:

                    // Log the exception.
                    Debug.LogError($"{nameof(SaveToFileAsync)} Error : {ex.Message}");

                    // Close the file that is open for writing.
                    streamWriter.Close();

                    // Notify the subscrivers that the operation is complete.
                    onComplete?.Invoke(false);

                    // Do nothing else.
                    yield break;
                }

                // Complete this time slice of the operation.
                yield return null;
            }

            // Notify the subscrivers that the operation is complete.
            onComplete?.Invoke(true);
        }

        /// <summary>
        /// Write string data to a file in a non-blocking fashion.
        /// </summary>
        /// <typeparam name="T">The type of the object to read from file.</typeparam>
        /// <param name="filePath">The file path to read from.</param>
        /// <param name="onComplete">A function object to handle when the operation completes.</param>
        /// <param name="bytesPerStep">The number of bytes to process during a time slice.</param>
        /// <returns>The instance of the object read from file.</returns>
        public static IEnumerator LoadFromFileAsync<T>(string filePath, Action<T> onComplete, int bytesPerStep = 1024 * 1024)
        {
            // If the file at the path does not exist, then:
            if (!File.Exists(filePath))
            {
                // Log an error.
                Debug.LogError("File not found: " + filePath);

                // Do nothing else.
                yield break;
            }

            // Otherwise, the file at the path does exist.

            // Create a stream reader.
            StreamReader streamReader = new StreamReader(filePath);

            // Create a string builder.
            StringBuilder data = new StringBuilder();

            // Iterate until a condition terminates the loop.
            while (true)
            {
                try
                {
                    // Create a buffer to copy the chunks that will be read.
                    char[] buffer = new char[bytesPerStep];

                    // Attempt to read the chunk into the buffer.
                    // Cache the number of bytes actually read, which is never greater than the chunk.
                    int bytesRead = streamReader.Read(buffer, 0, bytesPerStep);

                    // If there were bytes that were read, then:
                    if (bytesRead > 0)
                    {
                        // Append it the formatted string to the builder.
                        data.Append(buffer, 0, bytesRead);
                    }
                    else
                    {
                        // Otherwise, there were no bytes read.

                        // Close the file that is open for reading.
                        streamReader.Close();

                        // Notify the subscribers that the operation is complete.
                        onComplete?.Invoke(JsonConvert.DeserializeObject<T>(data.ToString()));

                        // Break out of the while loop.
                        break;
                    }

                }
                catch (JsonException ex)
                {
                    // If there were any exceptions thrown:

                    // Log the exception.
                    Debug.LogError($"{nameof(LoadFromFileAsync)} Error : {ex.Message}");

                    // Close the file that is open for reading.
                    streamReader.Close();

                    // Break out of the while loop.
                    break;
                }

                // Complete this time slice of the operation.
                yield return null;
            }
        }

        /// <summary>
        /// Determines whether or not the file path should be ignored.
        /// </summary>
        /// <param name="filePath">The file path to validate.</param>
        /// <returns>A value indicating whether or not the file path should be ignored.</returns>
        public static bool ShouldIgnoreAsset(string filePath)
        {
            // If the file path is null, empty, contains nothing but whitespace,
            // or the file path locates a folder in the project, then:
            if (string.IsNullOrEmpty(filePath) ||
                string.IsNullOrWhiteSpace(filePath) ||
                AssetDatabase.IsValidFolder(filePath))
            {
                // The file path should be ignored.
                return true;
            }

            // Otherwise, the file path string has valid content and it does not locate a folder.

            // Extract the extension from the file path.
            var extension = Path.GetExtension(filePath);

            // If the file extension is equivalent to the following file type extensions, then the file is an asset that should be ignored.
            // If the file is an asset that should be ignored, then:
            if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".hlsl ", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".asmdef", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".asmref", StringComparison.OrdinalIgnoreCase))
            {
                // The file path should be ignored.
                return true;
            }

            // Otherwise, the file is an asset that should not be ignored.

            // If the project settings provider is in debug mode and the file is under the debug folder, then:
            if (ProjectSettingsProvider.DebugMode &&
                !filePath.Contains(ProjectSettingsProvider.DebugRootFolder, StringComparison.OrdinalIgnoreCase))
            {
                // The file path should be ignored.
                return true;
            }

            // Otherwise, the project settings provider is not in debug mode, or the dile is not under the debug folder.

            // The file path should not be ignored.
            return false;
        }

        /// <summary>
        /// Ensure that the directories in the file path exist.
        /// </summary>
        /// <param name="filePath">The file path to create directories for.</param>
        /// TODO: Add to a shared util Class
        public static void EnsureDirectoryExist(string filePath)
        {
            // Extract the directory path for the file.
            // If the file path is a directory it is the same, if the file path points at a file, it attempts to get the parent directory.
            string directoryPath = Path.GetDirectoryName(filePath);
            
            // If the name of the directory is valid and the directory does not already exist, then:;
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                // Attempt to create the directory.
                Directory.CreateDirectory(directoryPath);
            }
        }
        #endregion
    }
}
