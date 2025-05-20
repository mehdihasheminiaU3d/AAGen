using System;
using System.Collections.Generic;
using System.Linq;
using AAGen.AssetDependencies;
using UnityEngine;

namespace AAGen
{
    internal class RefinementCommandQueue : CommandQueue
    {
        readonly DataContainer m_DataContainer;
        
        public RefinementCommandQueue(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;
            Title = nameof(RefinementCommandQueue);
        }

        public override void PreExecute()
        {
            foreach (var refinementRule in m_DataContainer.Settings.RefinementRules) 
            {
                AddCommand(new ActionCommand(() => refinementRule.Execute(m_DataContainer), refinementRule.name));
            }
            
            EnqueueCommands();
        }
        
        public override void PostExecute()
        {
        }
    }
}