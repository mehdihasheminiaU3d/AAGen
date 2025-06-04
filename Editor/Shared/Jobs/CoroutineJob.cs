using System;
using System.Collections;

namespace AAGen.Shared
{
    /// <summary>
    /// Represents a task that is defined by an iterator block.
    /// </summary>
    public class CoroutineJob : IJob
    {
        #region Fields
        /// <summary>
        /// The function object that can be used to store an iterator block.
        /// </summary>
        protected Func<IEnumerator> _coroutineInvoker;
        #endregion

        #region Properties
        /// <summary>
        /// The name of the job.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A value indicating whether the task has been completed.
        /// </summary>
        public bool IsComplete { get; protected set; }
        #endregion

        #region Methods
        #region Constructors
        /// <summary>
        /// Creates a new instance of the <see cref="CoroutineJob"/> class.
        /// </summary>
        public CoroutineJob() :
            this(null, string.Empty)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CoroutineJob"/> class.
        /// </summary>
        /// <param name="coroutineInvoker">The function object that can be used to store an iterator block.</param>
        /// <param name="name">The name of the job.</param>
        public CoroutineJob(Func<IEnumerator> coroutineInvoker, string name = null)
        {
            _coroutineInvoker = coroutineInvoker;
            Name = name;
        }
        #endregion

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <returns>A sequence of incremental sub-routines that the entire task is comprised of.</returns>
        public virtual IEnumerator Run()
        {
            // Set the value that indicates the job is incomplete.
            IsComplete = false;

            // If a iterator block instance is valid, then::
            if (_coroutineInvoker != null)
            {
                // Instantiate the iterator block.
                var coroutine = _coroutineInvoker.Invoke();

                // Begin to iterate the sequence of sub-routines in the iterator block.
                yield return coroutine;
            }

            // Set the value that indicates the job is complete.
            IsComplete = true;
        }
        #endregion
    }
}
