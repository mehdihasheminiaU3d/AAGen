using System;
using System.Collections.Generic;
using UnityEngine;
using AAGen.Shared;

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
        [Header("Rules")]
        public List<InputFilterRule> InputFilterRules = new List<InputFilterRule>();
        public List<OutputRule> OutputRules = new List<OutputRule>();
        
        [Header("Cleanup")]
        [SerializeField]
        [Tooltip("Should the tool remove the addressable asset entries that used to be needed but no longer are")]
        bool m_RemoveUnnecessaryEntries = true;
        [SerializeField]
        bool m_RemoveEmptyGroups = true;
        
        [Header("Process")]
        [SerializeField]
        ProcessingStepID m_ProcessingSteps = (ProcessingStepID)~0;
        [SerializeField]
        bool m_RunInBackground;
        [SerializeField]
        bool m_CheckBuiltinSceneDuplicates = true;
        [SerializeField]
        bool m_SaveGraphOnDisk = false;
        
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
        public bool SaveGraphOnDisk => m_SaveGraphOnDisk;

        public bool RemoveEmptyGroups => m_RemoveEmptyGroups;

        public bool RemoveUnnecessaryEntries => m_RemoveUnnecessaryEntries;

        public void Validate()
        {
        }
    }
}
