using System.Collections.Concurrent;
using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Core.JobWorkerServices;

public class JobWorkerCollectionService : IJobWorkerCollectionService
{
    public IReadOnlyCollection<JobWorker> Workers => (IReadOnlyCollection<JobWorker>)_jobWorkers.Keys;

    private readonly ConcurrentDictionary<JobWorker, JobWorkerValues> _jobWorkers = new();

    public void AddJobWorker(JobWorker worker, Task task, CancellationTokenSource cancellationTokenSource)
    {
        _jobWorkers[worker] = new JobWorkerValues(task, cancellationTokenSource);
    }

    public void RemoveJobWorker(JobWorker worker)
    {
        _jobWorkers.Remove(worker, out _);
    }

    public JobWorker? GetJobWorker(Job job)
    {
        return _jobWorkers.Keys.SingleOrDefault(w => w.Job != null && w.Job.Id == job.Id);
    }

    public void CancelJobWorkerToken(JobWorker worker)
    {
        _jobWorkers[worker].CancellationTokenSource.Cancel();
    }

    public Task GetJobWorkerTask(JobWorker worker)
    {
        // return Task
        return _jobWorkers[worker].Task;
    }

    public IEnumerable<Job> GetActiveJobs()
    {
        return (IEnumerable<Job>)Workers
            .Select(w => w.Job);
    }

    private class JobWorkerValues
    {
        public Task Task { get; set; }

        public CancellationTokenSource CancellationTokenSource { get; set; }

        public JobWorkerValues(Task task, CancellationTokenSource cancellationTokenSource)
        {
            Task = task;
            CancellationTokenSource = cancellationTokenSource;
        }
    }
}
