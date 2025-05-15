using System;
using System.Collections.Generic;
using UnityEngine;
using AAGen.Shared;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.Serialization;

namespace AAGen
{
    [Flags]
    public enum ProcessingStepID
    {
        GenerateDependencyGraph = 1 << 0,
        RemoveScenesFromBuildProfile = 1 << 1,
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
        [Header("Rules")]
        public List<InputFilterRule> InputFilterRules = new List<InputFilterRule>();
        
        SubgraphCategoryID m_DefaultCategoryID;
        [SerializeField]
        List<SubgraphCategoryID> m_SubgraphCategoryIds = new List<SubgraphCategoryID>();
        
        AddressableGroupNamingRule m_DefaultNamingRule;
        [SerializeField]
        List<AddressableGroupNamingRule> m_NamingRules = new List<AddressableGroupNamingRule>();
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

        public List<SubgraphCategoryID> SubgraphCategoryIds
        {
            get
            {
                return new List<SubgraphCategoryID>(m_SubgraphCategoryIds) //ToDo: Inefficient
                {
                    DefaultCategoryID
                };
            }
        }

        public List<AddressableGroupNamingRule> NamingRules
        {
            get
            {
                return new List<AddressableGroupNamingRule>(m_NamingRules) //ToDo: Inefficient
                {
                    DefaultNamingRule
                };
            }
        }

        public SubgraphCategoryID DefaultCategoryID
        {
            get => m_DefaultCategoryID;
            set => m_DefaultCategoryID = value;
        }

        public AddressableGroupNamingRule DefaultNamingRule
        {
            get => m_DefaultNamingRule;
            set => m_DefaultNamingRule = value;
        }
    }
}
