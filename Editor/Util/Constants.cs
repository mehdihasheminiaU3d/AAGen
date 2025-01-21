using System.IO;
using UnityEngine;

namespace AAGen
{
    internal static class Constants
    {
        public const string PackageShortName = "AAGen"; 
        public const string PackageName = "Automated Addressable Grouping Tool";

        public static class Menus
        {
            public const string Root = "Tools/";

            public const int AAGenMenuPriority = 100;
            public const string AAGenRootMenuPath = Root;
            public const string AAGenMenuPath = AAGenRootMenuPath + PackageName;
            
            public const string AAGenRootProjectSettingsPath = "Project/";
            public const string AAGenProjectSettingsPath = AAGenRootProjectSettingsPath + PackageShortName;
            
            public const int DependencyGraphMenuPriority = 200;
            public const string DependencyGraphRootMenuPath = Root + "Dependency Graph/";
        }

        public static class ContextMenus
        {
            public const string Root = "AAGen/";
        }

        public static class FilePaths
        {
            public const string DefaultBootScene = "Assets/AAGen Scenes/boot.unity";
            public static string ScenePreprocess => Path.Combine(FolderPath, "ScenePreprocess.txt");
        }
        
        public static string FolderPath => Path.Combine(Application.persistentDataPath, "DependencyGraph");
        public static string DependencyGraphFilePath => Path.Combine(FolderPath, "DependencyGraph.txt");
    }
}
