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
        public List<InputFilterRule> InputFilterRules;
        public List<MergeRule> _MergeRules;
        public List<GroupLayoutRule> _GroupLayoutRules;
        [Space]
        [SerializeField]
        LogLevelID m_LogLevel = LogLevelID.OnlyErrors;

        public ProcessingStepID ProcessingSteps => m_ProcessingSteps;
        public LogLevelID LogLevel => m_LogLevel;
        public bool RunInBackground => m_RunInBackground;
    }
}
