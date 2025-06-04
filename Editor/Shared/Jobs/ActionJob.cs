using NUnit.Framework.Constraints;
using System;
using System.Collections;

namespace AAGen.Shared
{
    /// <summary>
    /// Represents a task that is defined by any delegate that performs an action and returns nothing..
    /// </summary>
    public class ActionJob : CoroutineJob
    {
        #region Fields
        /// <summary>
        /// A delegate that performs an action and returns nothing.
        /// </summary>
        public Action _Action;
        #endregion

        #region Methods
        /// <summary>
        /// Creates a new instance of the <see cref="ActionJob"/> class.
        /// </summary>
        /// <param name="action">A delegate that performs an action and returns nothing.</param>
        /// <param name="name">The name of the job.</param>
        public ActionJob(Action action, string name = null)
        {
            // Cache the action.
            _Action = action;

            // If the name is invalid, then use the name of the wrapper function.
            // Otherwise, the name is valid, so it should be used.
            Name = string.IsNullOrEmpty(name) ? nameof(InvokeAction) : name;

            // Cache the iterator block that is used to encapsulate the action.
            _coroutineInvoker = InvokeAction; 
        }

        /// <summary>
        /// Encapsulates the action to invoke.
        /// </summary>
        /// <returns>A sequence of incremental sub-routines that the entire task is comprised of.</returns>
        IEnumerator InvokeAction()
        {
            // Invoke the action, if it exists. 
            _Action?.Invoke();

            // Signify the end of the iterator block and do nothing else.
            yield break;
        }
        #endregion
    }
}
