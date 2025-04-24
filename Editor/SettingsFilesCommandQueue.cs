using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace AAGen
{
    internal class SettingsFilesCommandQueue : CommandQueue
    {
        readonly DataContainer m_DataContainer;
            
        const string DefaultSettingsPath = "Assets/AddressableAssetsData";
        const string DefaultSettingsName = "AddressableAssetSettings";
        
        const string DefaultAagenSettingsFolder = "Assets/AddressableAssetsData/AAGen/";

        public SettingsFilesCommandQueue(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;
            Title = nameof(SettingsFilesCommandQueue);
        }

        public override void PreExecute()
        {
            AddCommand(new ActionCommand(FindOrCreateDefaultAddressableSettings, nameof(FindOrCreateDefaultAddressableSettings)));
            AddCommand(new ActionCommand(FindOrCreateDefaultToolSettings, nameof(FindOrCreateDefaultToolSettings)));
            EnqueueCommands();
        }

        void FindOrCreateDefaultAddressableSettings()
        {
            if (AddressableSettingsExists()) 
                return; //the default addressable asset settings already created

            CreateDefaultAddressableSettings();
        }

        bool AddressableSettingsExists()
        {
            return AddressableAssetSettingsDefaultObject.Settings != null;
        }

        void CreateDefaultAddressableSettings()
        {
            EnsureDirectoryExists(DefaultSettingsPath);

            // Create Addressable settings
            AddressableAssetSettings settings = AddressableAssetSettings.Create(DefaultSettingsPath, DefaultSettingsName, true, true);
            AddressableAssetSettingsDefaultObject.Settings = settings;
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("Default addressable assets settings created at: " + DefaultSettingsPath);
        }

        void FindOrCreateDefaultToolSettings()
        {
            //Settings is loaded
            if (m_DataContainer.Settings != null)
            {
                Debug.Log($"Settings is loaded");
                return;
            }

            //Settings exists but not loaded, ask the user to provide one
            if (ToolSettingsExists())
                throw new Exception($"Cannot find AAGen settings file");
            
            //If a settings file doesn't exists in the project, create one with default settings
            CreateDefaultToolSettings();
            Debug.Log($"CreateDefaultToolSettings");
        }

        bool ToolSettingsExists()
        {
            var allSettings = FindAllToolSettingsInstances();

            return allSettings.Count > 0;
        }
        
        static List<AagenSettings> FindAllToolSettingsInstances() 
        {
            List<AagenSettings> results = new List<AagenSettings>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(AagenSettings).Name}");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AagenSettings asset = AssetDatabase.LoadAssetAtPath<AagenSettings>(path);
                if (asset != null)
                {
                    results.Add(asset);
                }
            }

            return results;
        }
        
        void CreateDefaultToolSettings()
        {
            EnsureDirectoryExists(DefaultAagenSettingsFolder);
            
            try
            {
                AssetDatabase.StartAssetEditing();
                
                var settingsFilePath = Path.Combine(DefaultAagenSettingsFolder, $"Default {nameof(AagenSettings)}.asset");
                var settings = ScriptableObject.CreateInstance<AagenSettings>();
                
                settings._InputFilterRules = new List<InputFilterRule>
                {
                    CreateDefaultInputRule(),
                    CreateHardIgnoreInputRule()
                };

                settings._MergeRules = new List<MergeRule>
                {
                    CreateMergeRule(CategoryId.SingleSources, CategoryId.SharedAssets),
                    CreateMergeRule(CategoryId.SingleSources, CategoryId.SharedSingles),
                    CreateMergeRule(CategoryId.SingleSources, CategoryId.SharedSingleSinks)
                };

                var defaultGroupTemplate = FindDefaultAddressableGroupTemplate();
                settings._GroupLayoutRules = new List<GroupLayoutRule>
                {
                    CreateGroupLayoutRule(CategoryId.ExclusiveToSingleSource, defaultGroupTemplate),
                    CreateGroupLayoutRule(CategoryId.Hierarchies, defaultGroupTemplate),
                    CreateGroupLayoutRule(CategoryId.SharedAssets, defaultGroupTemplate),
                    CreateGroupLayoutRule(CategoryId.SharedSingles, defaultGroupTemplate),
                    CreateGroupLayoutRule(CategoryId.SharedSingleSinks, defaultGroupTemplate),
                    CreateGroupLayoutRule(CategoryId.SingleAssets, defaultGroupTemplate),
                    CreateGroupLayoutRule(CategoryId.SingleSources, defaultGroupTemplate)
                };

                AssetDatabase.CreateAsset(settings, settingsFilePath); //<--- ToDo: should we notify users about the file overwriting!
                AssetDatabase.SaveAssets();

                m_DataContainer.SettingsFilePath = settingsFilePath;
                m_DataContainer.Settings = settings;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }

        InputFilterRule CreateDefaultInputRule()
        {
            var inputFilterRulePath = Path.Combine(DefaultAagenSettingsFolder, $"Default {nameof(InputFilterRule)}.asset");
            var inputFilterRule = ScriptableObject.CreateInstance<IgnoreAssetByPathRule>();
            inputFilterRule._IgnoreOnlySourceNodes = true;
            inputFilterRule._IgnorePathsExcept = new List<string> { "Assets/" };
            inputFilterRule._IgnorePaths = new List<string>
            {
                "/Editor/",
                "Assets/Plugins",
            };
            inputFilterRule._DontIgnorePaths = new List<string>();
            AssetDatabase.CreateAsset(inputFilterRule, inputFilterRulePath);
            AssetDatabase.SaveAssets();
            return inputFilterRule;
        }
        
        InputFilterRule CreateHardIgnoreInputRule()
        {
            var inputFilterRulePath = Path.Combine(DefaultAagenSettingsFolder, $"HardIgnore {nameof(InputFilterRule)}.asset");
            var inputFilterRule = ScriptableObject.CreateInstance<IgnoreAssetByPathRule>();
            inputFilterRule._IgnoreOnlySourceNodes = false;
            inputFilterRule._IgnorePathsExcept = new List<string>();
            inputFilterRule._IgnorePaths = new List<string>
            {
                "Assets/AddressableAssetsData",
                "Assets/StreamingAssets",
                "/Resources/",
                "Assets/Gizmos"
            };
            inputFilterRule._DontIgnorePaths = new List<string>();
            AssetDatabase.CreateAsset(inputFilterRule, inputFilterRulePath);
            AssetDatabase.SaveAssets();
            return inputFilterRule;
        }
        
        MergeRule CreateMergeRule(CategoryId from, CategoryId to)
        {
            var mergeRulePath = Path.Combine(DefaultAagenSettingsFolder, $"Default {nameof(MergeRule)} {from} To {to}.asset");
            var mergeRule = ScriptableObject.CreateInstance<AssetNameMergeRule>();
            mergeRule._OriginCategory = from;
            mergeRule._DestinationCategory = to;
            AssetDatabase.CreateAsset(mergeRule, mergeRulePath);
            AssetDatabase.SaveAssets();
            return mergeRule;
        }
        
        GroupLayoutRule CreateGroupLayoutRule(CategoryId categoryId, AddressableAssetGroupTemplate template)
        {
            var groupLayoutRulePath = Path.Combine(DefaultAagenSettingsFolder, $"Default {nameof(GroupLayoutRule)} for {categoryId}.asset");
            var groupLayoutRule = ScriptableObject.CreateInstance<GenericGroupLayoutRule>();
            groupLayoutRule._CategoryId = categoryId;
            groupLayoutRule._AddressableAssetGroupTemplate = template;
            AssetDatabase.CreateAsset(groupLayoutRule, groupLayoutRulePath);
            AssetDatabase.SaveAssets();
            return groupLayoutRule;
        }
        
        static AddressableAssetGroupTemplate FindDefaultAddressableGroupTemplate()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            if (settings == null)
            {
                Debug.LogError("AddressableAssetSettings not found. Ensure Addressables are initialized.");
                return null;
            }
            
            var templates = settings.GroupTemplateObjects;

            if (templates == null || templates.Count == 0)
            {
                Debug.LogWarning("No group templates found in Addressable Settings.");
                return null;
            }

            // Assuming the first template is the default one
            var defaultTemplate = templates[0];
            return defaultTemplate as AddressableAssetGroupTemplate;
        }
        
        static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}