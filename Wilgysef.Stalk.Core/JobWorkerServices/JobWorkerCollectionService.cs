using System.Collections.Concurrent;
using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.JobWorkerServices;

public class JobWorkerCollectionService : IJobWorkerCollectionService, ISingletonDependency
{
    public IReadOnlyCollection<IJobWorker> Workers => (IReadOnlyCollection<IJobWorker>)_jobWorkers.Keys;

    private readonly ConcurrentDictionary<IJobWorker, JobWorkerValues> _jobWorkers = new();

    public void AddJobWorker(IJobWorker worker, Task task, CancellationTokenSource cancellationTokenSource)
    {
        _jobWorkers[worker] = new JobWorkerValues(task, cancellationTokenSource);
    }

    public void RemoveJobWorker(IJobWorker worker)
    {
        _jobWorkers.Remove(worker, out _);
    }

    public IJobWorker? GetJobWorker(Job job)
    {
        return _jobWorkers.Keys.SingleOrDefault(w => w.Job != null && w.Job.Id == job.Id);
    }

    public void CancelJobWorkerToken(IJobWorker worker)
    {
        _jobWorkers[worker].CancellationTokenSource.Cancel();
    }

    public Task GetJobWorkerTask(IJobWorker worker)
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
