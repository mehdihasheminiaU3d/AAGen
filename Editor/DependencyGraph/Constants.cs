using System.IO;
using UnityEngine;

namespace AAGen.Editor.DependencyGraph
{
    internal static class Constants
    {
        public const string PackageShortName = "AAGen"; 
        public const string PackageFullName = "Automated Addressable Asset Grouping Tool";
        public const string PackageName = "Automated Addressable Grouping Tool";

        public static class Menus
        {
            public const string Root = "Tools/";

            public const int AAGenMenuPriority = 100;
            public const string AAGenRootMenuPath = Root;
            public const string AAGenMenuPath = AAGenRootMenuPath + PackageName;
            
            public const int DependencyGraphMenuPriority = 200;
            public const string DependencyGraphRootMenuPath = Root + "Dependency Graph/";
        }
        
        public static string FolderPath => Path.Combine(Application.persistentDataPath, "DependencyGraph");
        public static string DependencyGraphFilePath => Path.Combine(FolderPath, "DependencyGraph.txt");
    }
}
