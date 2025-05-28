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
            ClearQueue();
            AddCommand(CreateAddressableSettingsIfRequired, nameof(CreateAddressableSettingsIfRequired));
            AddCommand(FindOrCreateDefaultToolSettings, nameof(FindOrCreateDefaultToolSettings));
            AddCommand(() => m_DataContainer.Settings.Validate(), "Validate Settings");
        }

       public static void CreateAddressableSettingsIfRequired()
        {
            if (AddressableAssetSettingsDefaultObject.Settings == null)
                CreateDefaultAddressableSettings();
        }

        static void CreateDefaultAddressableSettings()
        {
            EnsureDirectoryExists(DefaultSettingsPath);

            // Create Addressable settings
            AddressableAssetSettings settings = AddressableAssetSettings.Create(DefaultSettingsPath, DefaultSettingsName, true, true);
            AddressableAssetSettingsDefaultObject.Settings = settings;
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        void FindOrCreateDefaultToolSettings()
        {
            //Settings is loaded
            if (m_DataContainer.Settings != null)
                return;

            //Settings exists but not loaded, ask the user nicely to provide one
            if (ToolSettingsExists())
                throw new Exception($"Cannot find AAGen settings file");
            
            //If a settings file doesn't exists in the project, create one with default settings
            CreateDefaultToolSettings();
        }
        
        void CreateDefaultToolSettings()
        {
            EnsureDirectoryExists(DefaultAagenSettingsFolder);
            var settingsFilePath = Path.Combine(DefaultAagenSettingsFolder, $"Default {nameof(AagenSettings)}.asset");
            var settings = CreateDefaultToolSettingsAtPath(settingsFilePath);
            
            m_DataContainer.SettingsFilePath = settingsFilePath;
            m_DataContainer.Settings = settings;
        }
        
        public static AagenSettings CreateDefaultToolSettingsAtPath(string settingsFilePath)
        {
            var directory = Path.GetDirectoryName(settingsFilePath);
            
            AagenSettings settings = null;
            
            try
            {
                AssetDatabase.StartAssetEditing();
                
                settings = ScriptableObject.CreateInstance<AagenSettings>();
                
                settings.InputFilterRules = new List<InputFilterRule>
                {
                    CreateDefaultInputRule(directory)
                };
                
                settings.OutputRules = new List<OutputRule>
                {
                    CreateDefaultOutputRule(directory)
                };
                
                var defaultGroupTemplate = FindDefaultAddressableGroupTemplate();
                if (defaultGroupTemplate == null)
                    throw new Exception($"cannot find default addressable group template");
                settings.m_DefaultGroupTemplate = defaultGroupTemplate;

                AssetDatabase.CreateAsset(settings, settingsFilePath);
                AssetDatabase.SaveAssets();
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

            return settings;
        }

        static InputFilterRule CreateDefaultInputRule(string directoryPath)
        {
            var inputFilterRulePath = Path.Combine(directoryPath, $"Default {nameof(InputFilterRule)}.asset");
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
        
        static OutputRule CreateDefaultOutputRule(string directoryPath)
        {
            var outputRule = new OutputRule
            {
                Name = "Default Output Rule"
            };
            
            var subgraphSelectorPath = Path.Combine(directoryPath, $"{nameof(DefaultSubgraphSelector)}.asset");
            var subgraphSelectorRule = ScriptableObject.CreateInstance<DefaultSubgraphSelector>();
            AssetDatabase.CreateAsset(subgraphSelectorRule, subgraphSelectorPath);
            outputRule.SubgraphSelector = subgraphSelectorRule;
            
            var refinementRulePath = Path.Combine(directoryPath, $"{nameof(DefaultRefinementRule)}.asset");
            var refinementRule = ScriptableObject.CreateInstance<DefaultRefinementRule>();
            AssetDatabase.CreateAsset(refinementRule, refinementRulePath);
            outputRule.RefinementRule = refinementRule;
            
            var namingRulePath = Path.Combine(directoryPath, $"{nameof(DefaultNamingRule)}.asset");
            var namingRule = ScriptableObject.CreateInstance<DefaultNamingRule>();
            AssetDatabase.CreateAsset(namingRule, namingRulePath);
            outputRule.NamingRule = namingRule;
            
            AssetDatabase.SaveAssets();
     
            return outputRule;
        }
        
        static AddressableAssetGroupTemplate FindDefaultAddressableGroupTemplate()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;

            if (settings == null)
                throw new ("AddressableAssetSettings not found. Ensure Addressables are initialized.");
            
            var templates = settings.GroupTemplateObjects;

            if (templates == null || templates.Count == 0)
               throw new ("No group templates found in Addressable Settings.");
            
            // Assuming the first template is the default one
            var defaultTemplate = templates[0];
            return defaultTemplate as AddressableAssetGroupTemplate;
        }

        #region Utils

        static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
        
        static bool ToolSettingsExists()
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

        #endregion
    }
}