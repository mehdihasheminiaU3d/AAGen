using System.IO;
using UnityEngine;

namespace AAGen.Editor.DependencyGraph
{
    internal static class DependencyGraphConstants
    {
        public static string FolderPath => Path.Combine(Application.persistentDataPath, "DependencyGraph");
        public static string DependencyGraphFilePath => Path.Combine(FolderPath, "DependencyGraph.txt");
    }
}
