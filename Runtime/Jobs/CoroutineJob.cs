using System;
using System.Collections;

namespace AAGen.Runtime
{
    public class CoroutineJob : IJob
    {
        protected Func<IEnumerator> _coroutineInvoker;
        public string Name { get; set; }
        public bool IsComplete { get; protected set; }
        
        public CoroutineJob()
        {
            Name = string.Empty;
        }
        
        public CoroutineJob(Func<IEnumerator> coroutineInvoker, string name = null)
        {
            _coroutineInvoker = coroutineInvoker;
            Name = name;
        }

        public virtual IEnumerator Run()
        {
            IsComplete = false;

            if (_coroutineInvoker != null)
            {
                var coroutine = _coroutineInvoker.Invoke();
                yield return coroutine;
            }
            
            IsComplete = true;
        }
    }
}
