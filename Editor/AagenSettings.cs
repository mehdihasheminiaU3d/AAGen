using System;
using System.Collections.Generic;
using UnityEngine;
using AAGen.Shared;

namespace AAGen
{
    [Flags]
    public enum ProcessingStepId
    {
        GenerateDependencyGraph = 1 << 0,
        RemoveBuildProfileScenes = 1 << 1,
        AssetIntakeFilter = 1 << 2,
        GenerateSubGraphs = 1 << 3,
        GenerateGroupLayout = 1 << 4,
        Cleanup = 1 << 5
    }

    public enum LogLevel
    {
        Error,
        Developer,
    }

    [CreateAssetMenu(menuName = Constants.ContextMenus.Root + "Settings")]
    internal class AagenSettings : ScriptableObject
    {
        [SerializeField]
        ProcessingStepId m_ProcessingSteps = (ProcessingStepId)~0;

        public LogLevel m_LogLevel;

        public bool m_RunInBackground;
        
        [Space]
        public List<InputFilterRule> _InputFilterRules;
        public List<MergeRule> _MergeRules;
        public List<GroupLayoutRule> _GroupLayoutRules;
    }
}
