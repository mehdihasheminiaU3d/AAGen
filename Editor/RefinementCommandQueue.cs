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
                AddCommand(() => refinementRule.Execute(m_DataContainer), refinementRule.name);
            }
        }
        
        public override void PostExecute()
        {
        }
    }
}