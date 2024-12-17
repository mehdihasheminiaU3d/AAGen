using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AAGen.Runtime;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace AAGen.Editor.DependencyGraph
{
    internal class DefaultSystemSetupCreator : DependencyGraphProcessor
    {
        public DefaultSystemSetupCreator(DependencyGraph dependencyGraph, EditorUiGroup uiGroup, AutomatedAssetGrouper parentUi) : base(dependencyGraph, uiGroup)
        {
            _parentUi = parentUi;
        }
        
        private AutomatedAssetGrouper _parentUi;
        EditorJobGroup _sequence;
        
        const string DefaultSettingsPath = "Assets/AddressableAssetsData";
        const string DefaultSettingsName = "AddressableAssetSettings";
        
        const string DefaultAagenSettingsFolder = "Assets/AddressableAssetsData/AAGen/";
        
        public void CreateDefaultSettingsFiles()
        {
            _sequence = new EditorJobGroup(nameof(GraphInfoProcessor));
            _sequence.AddJob(new ActionJob(Init, nameof(Init)));
            _sequence.AddJob(new ActionJob(InitializeAddressables, nameof(InitializeAddressables)));
            _sequence.AddJob(new ActionJob(CreateDefaultSettingsFile, nameof(CreateDefaultSettingsFile)));
            EditorCoroutineUtility.StartCoroutineOwnerless(_sequence.Run());
        }
        
        void InitializeAddressables()
        {
            // Step 1: Check if Addressables Settings exist
            var settings = AddressableAssetSettingsDefaultObject.Settings;

            if (settings == null)
            {
                Debug.Log("Addressables settings not found. Creating new Addressables settings...");
                settings = CreateDefaultAddressableSettings();
            }
        }
        
        static AddressableAssetSettings CreateDefaultAddressableSettings()
        {
            // Ensure directory exists
            if (!Directory.Exists(DefaultSettingsPath))
            {
                Directory.CreateDirectory(DefaultSettingsPath);
            }

            // Create Addressable settings
            AddressableAssetSettings settings = AddressableAssetSettings.Create(
                DefaultSettingsPath, DefaultSettingsName, true, true
            );

            // Add a default group
            var group = settings.CreateGroup("Default Local Group", false, false, true, null, //<---------Is this needed?
                typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));
            settings.DefaultGroup = group;

            Debug.Log("Addressables settings created at: " + DefaultSettingsPath);
            return settings;
        }

        void CreateDefaultSettingsFile()
        {
            try
            {
                AssetDatabase.StartAssetEditing();

                // Ensure directory exists
                if (!Directory.Exists(DefaultAagenSettingsFolder))
                {
                    Directory.CreateDirectory(DefaultAagenSettingsFolder);
                }

                var settingsFilePath = Path.Combine(DefaultAagenSettingsFolder, $"Default {nameof(AagSettings)}.asset");
                var aagSettings = ScriptableObject.CreateInstance<AagSettings>();
                
                aagSettings._InputFilterRules = new List<InputFilterRule>
                {
                    CreateDefaultInputRule()
                };

                aagSettings._MergeRules = new List<MergeRule>
                {
                    CreateMergeRule(CategoryId.SingleSources, CategoryId.SharedAssets),
                    CreateMergeRule(CategoryId.SingleSources, CategoryId.SharedSingles),
                    CreateMergeRule(CategoryId.SingleSources, CategoryId.SharedSingleSinks)
                };

                var defaultGroupTemplate = FindDefaultAddressableGroupTemplate();
                aagSettings._GroupLayoutRules = new List<GroupLayoutRule>
                {
                    CreateGroupLayoutRule(CategoryId.ExclusiveToSingleSource, defaultGroupTemplate),
                    CreateGroupLayoutRule(CategoryId.Hierarchies, defaultGroupTemplate),
                    CreateGroupLayoutRule(CategoryId.SharedAssets, defaultGroupTemplate),
                    CreateGroupLayoutRule(CategoryId.SharedSingles, defaultGroupTemplate),
                    CreateGroupLayoutRule(CategoryId.SharedSingleSinks, defaultGroupTemplate),
                    CreateGroupLayoutRule(CategoryId.SingleAssets, defaultGroupTemplate),
                    CreateGroupLayoutRule(CategoryId.SingleSources, defaultGroupTemplate)
                };

                AssetDatabase.CreateAsset(aagSettings, settingsFilePath); //<--- ToDo: Overwrite notification!
                AssetDatabase.SaveAssets();
                
                _parentUi.AagSettings = aagSettings;
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
            inputFilterRule._IgnorePaths = new List<string> { "/Editor/" };
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
        
        public static AddressableAssetGroupTemplate FindDefaultAddressableGroupTemplate()
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
    }
}