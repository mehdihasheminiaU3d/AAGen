using System;
using System.Collections.Generic;
using UnityEngine;
using AAGen.Shared;
using UnityEditor.AddressableAssets.Settings;

namespace AAGen
{
    [Flags]
    public enum ProcessingStepID
    {
        GenerateDependencyGraph = 1 << 0,
        AssetInputFilter = 1 << 2,
        GenerateSubGraphs = 1 << 3,
        GenerateGroupLayout = 1 << 4,
        GenerateAddressableGroups = 1 << 5,
        Cleanup = 1 << 6
    }

    public enum LogLevelID
    {
        OnlyErrors = 0,  //Only unexpected errors
        Info       = 1,  //Detailed informational messages
        Developer  = 2   //Extremely detailed messages
    }

    [CreateAssetMenu(menuName = Constants.ContextMenus.Root + "Settings")]
    public class AagenSettings : ScriptableObject
    {
        [Header("Process")]
        [SerializeField]
        ProcessingStepID m_ProcessingSteps = (ProcessingStepID)~0;
        [SerializeField]
        bool m_RunInBackground;
        public AddressableAssetGroupTemplate m_DefaultGroupTemplate;
        [SerializeField]
        bool m_CheckBuiltinSceneDuplicates = true;
        [SerializeField]
        bool m_SaveGraphOnDisk = false;
        [Header("Rules")]
        public List<InputFilterRule> InputFilterRules = new List<InputFilterRule>();
        [SerializeField]
        List<SubgraphCategoryID> m_SubgraphCategoryIds = new List<SubgraphCategoryID>();
        [SerializeField]
        List<AddressableGroupNamingRule> m_NamingRules = new List<AddressableGroupNamingRule>();
        [SerializeField]
        List<RefinementRule> m_RefinementRules = new List<RefinementRule>();
        [Header("Reports")]
        [SerializeField]
        bool m_GenerateSummaryReport;
        [SerializeField]
        LogLevelID m_LogLevel = LogLevelID.OnlyErrors;
        
        [Header("Obsolete")]
        public List<MergeRule> _MergeRules;
        public List<GroupLayoutRule> _GroupLayoutRules;
        
        public ProcessingStepID ProcessingSteps => m_ProcessingSteps;
        public LogLevelID LogLevel => m_LogLevel;
        public bool RunInBackground => m_RunInBackground;

        public bool GenerateSummaryReport => m_GenerateSummaryReport;

        public List<SubgraphCategoryID> SubgraphCategoryIds => m_SubgraphCategoryIds;
        public List<AddressableGroupNamingRule> NamingRules => m_NamingRules;

        public void Validate()
        {
        }
        
        SubgraphCategoryID m_DefaultCategoryID;
        public SubgraphCategoryID DefaultCategoryID
        {
            get
            {
                if (m_DefaultCategoryID == null)
                {
                    m_DefaultCategoryID = CreateInstance<UncategorizedSubgraphID>();
                    m_DefaultCategoryID.name = $"Uncategorized";
                }
                return m_DefaultCategoryID;
            }
        }

        AddressableGroupNamingRule m_DefaultNamingRule;
        public AddressableGroupNamingRule DefaultNamingRule
        {
            get
            {
                if (m_DefaultNamingRule == null)
                {
                    m_DefaultNamingRule = CreateInstance<DefaultNamingRule>();  
                    m_DefaultNamingRule.name = $"DefaultNamingRule";
                    m_DefaultNamingRule.m_CategoryID = DefaultCategoryID;
                }
                
                return m_DefaultNamingRule;
            }
        }

        public List<RefinementRule> RefinementRules => m_RefinementRules;

        public bool SaveGraphOnDisk => m_SaveGraphOnDisk;
    }
}
