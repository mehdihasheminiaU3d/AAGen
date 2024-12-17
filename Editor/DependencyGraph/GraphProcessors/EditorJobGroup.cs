using System.Collections;
using AAGen.Runtime;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace AAGen.Editor
{
    /// <summary>
    /// A job group for the editor with the ability to display progress.
    /// </summary>
    public class EditorJobGroup: JobGroup
    {
        private int _progressId;
        private string _name;
        
        private float _currentJobProgress;
        private string _currentJobDescription;

        private readonly bool _IsCancellable;
        private bool _isCancelled;
        
        /// <summary>
        /// IsCancellable = false is almost 5X faster!
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isCancellable"></param>
        public EditorJobGroup(string name, bool isCancellable = false)
        {
            _name = name;
            _IsCancellable = isCancellable;
        }

        public EditorJobGroup(string name, bool isCancellable, params IJob[] jobs) : base(jobs)
        {
            _name = name;
            _IsCancellable = isCancellable;
        }

        public override IEnumerator Run()
        {
            StartProgressBar(_name);
            IsComplete = false;
            _isCancelled = false;

            for (var i = 0; i < Jobs.Count; i++)
            {
                _currentJobProgress = 0f;
                _currentJobDescription = null;
                
                var job = Jobs[i];
                
                var currentJob = EditorCoroutineUtility.StartCoroutineOwnerless(job.Run());
                
                while(!job.IsComplete)
                {
                    float progress = (i + _currentJobProgress) / Jobs.Count;
                    UpdateProgressBar(progress, _currentJobDescription);
                    yield return null;

                    if (_isCancelled)
                    {
                        EditorCoroutineUtility.StopCoroutine(currentJob);
                        currentJob = null;
                        break;
                    }
                }

                if (_isCancelled)
                {
                    Debug.Log($"Job={_name} Cancelled!");
                    break;
                }
            }

            _isCancelled = false;
            IsComplete = true;
            ResetProgressBar();
        }
        
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

        private void UpdateProgressBar(float progress, string info)
        {
            if(_IsCancellable)
            {
                if (EditorUtility.DisplayCancelableProgressBar(_name, info, progress))
                    _isCancelled = true;
            }
            else
            {
                Progress.Report(_progressId, progress, info);
            }
        }

        private void ResetProgressBar()
        {
            if(_IsCancellable)
            {
                EditorUtility.ClearProgressBar();
            }
            else
            {
                if (Progress.Exists(_progressId))
                    Progress.Remove(_progressId);
            }
        }

        public void ReportProgress(float value, string description)
        {
            _currentJobProgress = Mathf.Clamp01(value);
            _currentJobDescription = description;
        }

        public void Cancel()
        {
            _isCancelled = true;
        }
    }
}
