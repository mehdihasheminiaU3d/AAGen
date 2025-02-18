using System.Collections;
using System.Collections.Generic;

namespace AAGen.Shared
{
    public abstract class JobGroup : IJob
    {
        public string Name { get; set; } = null;
        
        public List<IJob> Jobs { get; protected set; } = new List<IJob>();
        public bool IsComplete { get; protected set; }
        public abstract IEnumerator Run();

        public JobGroup()
        {
        }
        
        public JobGroup(IJob[] jobs)
        {
            AddJobs(jobs);
        }

        public void AddJobs(IJob[] jobs)
        {
            foreach (var job in jobs)
            {
                AddJob(job);
            }
        }
        
        public void AddJob(IJob job)
        {
            if(!Jobs.Contains(job))
                Jobs.Add(job);
        }
    }
}
