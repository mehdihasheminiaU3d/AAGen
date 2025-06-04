using System.Collections;
using System.Collections.Generic;

namespace AAGen.Shared
{
    public abstract class JobGroup : IJob
    {
        #region Properties
        /// <summary>
        /// The name of the job.
        /// </summary>
        public string Name { get; set; } = null;
        
        /// <summary>
        /// The collection of jobs that this job is composed of.
        /// </summary>
        public List<IJob> Jobs { get; protected set; } = new List<IJob>();

        /// <summary>
        /// A value indicating whether the task has been completed.
        /// </summary>
        public bool IsComplete { get; protected set; }
        #endregion

        #region Methods
        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <returns>A sequence of incremental sub-routines that the entire task is comprised of.</returns>
        public abstract IEnumerator Run();

        #region Constructors
        /// <summary>
        /// Creates a new instance of the <see cref="JobGroup"/> class.
        /// </summary>
        public JobGroup()
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="JobGroup"/> class.
        /// </summary>
        /// <param name="jobs">The collection of jobs that this job is composed of.</param>
        public JobGroup(IJob[] jobs)
        {
            // Add the jobs to the collection of jobs that this job is composed of.
            AddJobs(jobs);
        }
        #endregion

        /// <summary>
        /// Add a collection of jobs to the jobs that this job is composed of.
        /// </summary>
        /// <param name="jobs">The collection of jobs to add.</param>
        public void AddJobs(IJob[] jobs)
        {
            // For every job in the list of jobs to add, perform the following:
            foreach (var job in jobs)
            {
                // Add the job to the list of jobs that this job is composed of.
                AddJob(job);
            }
        }

        /// <summary>
        /// Add a job to the jobs that this job is composed of.
        /// </summary>
        /// <param name="jobs">The job to add.</param>
        public void AddJob(IJob job)
        {
            // If the list of jobs does not contain this job, then: 
            if (!Jobs.Contains(job))
            {
                // Add tthe job to the list of jobs that this job is composed of.
                Jobs.Add(job);
            }
        }
        #endregion
    }
}
