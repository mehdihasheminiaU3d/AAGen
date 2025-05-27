using System;
using System.Collections.Generic;
using System.Linq;
using AAGen.AssetDependencies;
using UnityEngine;

namespace AAGen
{
    internal class RefinementCommandQueue : NewCommandQueue
    {
        readonly DataContainer m_DataContainer;
        
        public RefinementCommandQueue(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;
            Title = nameof(RefinementCommandQueue);
        }

        public override void PreExecute()
        {
            ClearQueue();
            foreach (var refinementRule in m_DataContainer.Settings.RefinementRules)
            {
                var rule = refinementRule;
                AddCommand(() => rule.Execute(m_DataContainer), rule.name);
            }
        }
        
        public override void PostExecute()
        {
        }
    }
}