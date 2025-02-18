using System;
using System.Collections;

namespace AAGen.Shared
{
    public class ActionJob : CoroutineJob
    {
        public Action _Action;
        
        public ActionJob(Action action, string name = null)
        {
            _Action = action;
            Name = string.IsNullOrEmpty(name) ? nameof(InvokeAction) : name;
            _coroutineInvoker = InvokeAction;
        }
        
        IEnumerator InvokeAction()
        {
            _Action?.Invoke();
            yield break;
        }
    }
}
