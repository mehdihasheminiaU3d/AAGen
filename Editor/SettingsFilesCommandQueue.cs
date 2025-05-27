using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace AAGen
{
    internal class SettingsFilesCommandQueue : NewCommandQueue
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
            ClearQueue();
            AddCommand(FindOrCreateDefaultAddressableSettings, nameof(FindOrCreateDefaultAddressableSettings));
            AddCommand(FindOrCreateDefaultToolSettings, nameof(FindOrCreateDefaultToolSettings));
            AddCommand(() => m_DataContainer.Settings.Validate(), "Validate Settings");
        }

        void FindOrCreateDefaultAddressableSettings()
        {
            if (AddressableSettingsExists())
            {
                m_DataContainer.AddressableSettings = AddressableAssetSettingsDefaultObject.Settings;
                return; //the default addressable asset settings already created
            }

            CreateDefaultAddressableSettings();
            
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                throw new Exception($"Addressable Asset Settings not found!");

            m_DataContainer.AddressableSettings = settings;
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
            
            // m_DataContainer.Logger.LogInfo(this, "Default addressable assets settings created at: " + DefaultSettingsPath); //ToDo: settings might not exists yet!
        }

        void FindOrCreateDefaultToolSettings()
        {
            //Settings is loaded
            if (m_DataContainer.Settings != null)
                return;

            //Settings exists but not loaded, ask the user to provide one
            if (ToolSettingsExists())
                throw new Exception($"Cannot find AAGen settings file");
            
            //If a settings file doesn't exists in the project, create one with default settings
            CreateDefaultToolSettings();
            
            // m_DataContainer.Logger.LogInfo(this,$"CreateDefaultToolSettings"); //ToDo: sometimes doesn't work immediately after creating the settings
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
                
                settings.InputFilterRules = new List<InputFilterRule>
                {
                    CreateDefaultInputRule()
                };

                settings._MergeRules = new List<MergeRule>
                {
                    CreateMergeRule(CategoryId.SingleSources, CategoryId.SharedAssets),
                    CreateMergeRule(CategoryId.SingleSources, CategoryId.SharedSingles),
                    CreateMergeRule(CategoryId.SingleSources, CategoryId.SharedSingleSinks)
                };

                var defaultGroupTemplate = FindDefaultAddressableGroupTemplate();
                if (defaultGroupTemplate == null)
                    throw new Exception($"cannot find default addressable group template");
                settings.m_DefaultGroupTemplate = defaultGroupTemplate;
                
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
            var inputFilterRule = ScriptableObject.CreateInstance<PathFilterRule>();
            inputFilterRule.m_Criteria = new List<PathFilterCriterion>
            {
                new PathFilterCriterion()
                {
                    m_Description = "Ignore all except files in Assets folder",
                    m_PathMatchCondition = PathMatchCondition.DoesNotContain,
                    m_PathKeyword = "Assets/",
                    m_InclusionAction = InclusionAction.IgnoreIfSourceOnly,
                },
                new PathFilterCriterion()
                {
                    m_Description = "Ignore assets in Editor folders",
                    m_PathMatchCondition = PathMatchCondition.Contains,
                    m_PathKeyword = "/Editor/",
                    m_InclusionAction = InclusionAction.IgnoreIfSourceOnly,
                },
                new PathFilterCriterion()
                {
                    m_Description = "Ignore Plugins",
                    m_PathMatchCondition = PathMatchCondition.Contains,
                    m_PathKeyword = "Assets/Plugins",
                    m_InclusionAction = InclusionAction.IgnoreIfSourceOnly,
                },
                new PathFilterCriterion() 
                {
                    m_Description = "Ignore Addressable data files",
                    m_PathMatchCondition = PathMatchCondition.Contains,
                    m_PathKeyword = "Assets/AddressableAssetsData",
                    m_InclusionAction = InclusionAction.AlwaysIgnore,
                },
                new PathFilterCriterion()
                {
                    m_Description = "Ignore assets in StreamingAssets folder",
                    m_PathMatchCondition = PathMatchCondition.Contains,
                    m_PathKeyword = "Assets/StreamingAssets",
                    m_InclusionAction = InclusionAction.AlwaysIgnore,
                },
                new PathFilterCriterion()
                {
                    m_Description = "Ignore Resources",
                    m_PathMatchCondition = PathMatchCondition.Contains,
                    m_PathKeyword = "/Resources/",
                    m_InclusionAction = InclusionAction.AlwaysIgnore,
                }
            };
            
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