using System.Collections.Generic;
using UnityEngine;

namespace AAGen
{
    public abstract class RefinementRule : ScriptableObject
    {
        public abstract void Execute(DataContainer dataContainer);
    }
}