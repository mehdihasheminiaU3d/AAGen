using System.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace AAGen.Shared
{
    /// <summary>
    /// A <see cref="JobGroup"/> that can display its progress through the Unity editor.
    /// </summary>
    public class EditorJobGroup: JobGroup
    {
        #region Fields
        /// <summary>
        /// The identifier issued by the Unitym, which is used to keep track of the progress bar for update.
        /// </summary>
        /// <remarks>
        /// Only used for non-cancellable progress bars.
        /// </remarks>
        private int _progressId;

        /// <summary>
        /// The name of the progress bar.
        /// </summary>
        private string _name;
        
        /// <summary>
        /// The progress of this job, which is presented on the progress bar.
        /// </summary>
        private float _currentJobProgress;

        /// <summary>
        /// The description of the current work being done, which is presented on the progress bar.
        /// </summary>
        private string _currentJobDescription;

        /// <summary>
        /// A value indicating whether the progress bar should allow the user to cancel the job.
        /// </summary>
        private readonly bool _IsCancellable;

        /// <summary>
        /// A value indicating that the group should be cancelled.
        /// </summary>
        private bool _isCancelled;
        #endregion

        #region Methods
        #region Constructors
        /// <summary>
        /// Creates a new instance of the <see cref="EditorJobGroup"/> class.
        /// </summary>
        /// <param name="name">The name of job.</param>
        /// <param name="isCancellable">A value indicating whether the progress bar should allow the user to cancel the job.</param>
        /// <param name="jobs">The collection of jobs that this job is composed of.</param>
        /// <remarks>
        /// IsCancellable = false is almost 5X faster!
        /// </remarks>
        public EditorJobGroup(string name, bool isCancellable = false, params IJob[] jobs) : base(jobs)
        {
            _name = name;
            _IsCancellable = isCancellable;
        }
        #endregion

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <returns>
        /// A sequence of incremental sub-routines that the entire task is comprised of.
        /// </returns>
        public override IEnumerator Run()
        {
            // Start the progress bar display in the Unity editor.
            StartProgressBar(_name);


            IsComplete = false;
            _isCancelled = false;

            // For every job in the list of jobs this job is composed of, perform the following:
            for (var i = 0; i < Jobs.Count; i++)
            {
                // Set the progress and description to before any jobs are performed.
                _currentJobProgress = 0f;
                _currentJobDescription = null;
                
                var job = Jobs[i];
                
                // Create an Editor coroutine.
                var currentJob = EditorCoroutineUtility.StartCoroutineOwnerless(job.Run());
                
                // While the job has not been completed, perform the following:
                while(!job.IsComplete)
                {
                    // Progress increments are equal across all jobs.
                    float progress = (i + _currentJobProgress) / Jobs.Count;

                    // Update the display with the new progress.
                    UpdateProgressBar(progress, _currentJobDescription);
                    
                    yield return null;

                    // If the user cancelled the job through the Editor controls, then:
                    if (_isCancelled)
                    {
                        // End the coroutine for the current joib.
                        EditorCoroutineUtility.StopCoroutine(currentJob);
                        currentJob = null;
                    
                        break;
                    }
                }

                // The current job same been interrupted or completed.

                // If the user cancelled the job through the Editor controls, then:
                if (_isCancelled)
                {
                    // Log a noitification that the job was cancelled. 
                    Debug.Log($"Job={_name} Cancelled!");
                
                    // Do not process any other jobs.
                    break;
                }
            }

            // The job is marked complete. 
            _isCancelled = false;
            IsComplete = true;

            // Clear the progress bar display.
            ResetProgressBar();
        }

        /// <summary>
        /// Start the progress bar display in the Unity editor.
        /// </summary>
        /// <param name="title"></param>
        private void StartProgressBar(string title)
        {
            ResetProgressBar();
            
            if(_IsCancellable)
            {
                EditorUtility.DisplayCancelableProgressBar(_name, null, 0f);
            }
            else
            {
                _progressId = Progress.Start(title);
            }
        }

        /// <summary>
        /// Updates the progress bar in the Unity editor with the latest progress and description of work.
        /// </summary>
        /// <param name="progress">The progress of this job, which is presented on the progress bar.</param>
        /// <param name="description">The description of the current work being done, which is presented on the progress bar.</param>
        private void UpdateProgressBar(float progress, string description)
        {
            if(_IsCancellable)
            {
                if (EditorUtility.DisplayCancelableProgressBar(_name, description, progress))
                {
                    _isCancelled = true;
                }
            }
            else
            {
                Progress.Report(_progressId, progress, description);
            }
        }

        /// <summary>
        /// Removes the progress bar from being displayed in the Unity editor.
        /// </summary>
        private void ResetProgressBar()
        {
            if(_IsCancellable)
            {
                EditorUtility.ClearProgressBar();
            }
            else if (Progress.Exists(_progressId))
            {
                Progress.Remove(_progressId);
            }
        }

        /// <summary>
        /// Sets the progress and description so that the display can be updated when the job is ready to.
        /// </summary>
        /// <param name="value">The progress value.</param>
        /// <param name="description">The description of the work.</param>
        public void ReportProgress(float value, string description)
        {
            _currentJobProgress = Mathf.Clamp01(value);
            _currentJobDescription = description;
        }

        /// <summary>
        /// Interrupts and cancels the jobs betwwewen coroutine increments.
        /// </summary>
        public void Cancel()
        {
            _isCancelled = true;
        }
        #endregion
    }
}
