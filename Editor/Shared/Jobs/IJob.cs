using System.Collections;

namespace AAGen.Shared
{
    /// <summary>
    /// Provides an interface for an asynchronous task. 
    /// </summary>
    public interface IJob
    {
        #region Properties
        /// <summary>
        /// The name of the job.
        /// </summary>
        string Name { get; set; }
        
        /// <summary>
        /// A value indicating whether the task has been completed.
        /// </summary>
        bool IsComplete { get; }
        #endregion

        #region Methods
        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <returns>A sequence of incremental sub-routines that the entire task is comprised of.</returns>
        IEnumerator Run();
        #endregion
    }
}
