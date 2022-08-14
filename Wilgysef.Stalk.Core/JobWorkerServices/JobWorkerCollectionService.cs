using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Core.JobWorkerServices;

public class JobWorkerCollectionService : IJobWorkerCollectionService
{
    public IReadOnlyCollection<JobWorker> Workers => _jobWorkerObjects.Keys;

    public IReadOnlyCollection<Job> Jobs => (IReadOnlyCollection<Job>)Workers
        .Select(w => w.Job)
        .Where(j => j != null)
        .ToList();

    private readonly Dictionary<JobWorker, JobWorkerObjects> _jobWorkerObjects = new();

    public void AddJobWorker(JobWorker worker, Task task, CancellationTokenSource cancellationTokenSource)
    {
        _jobWorkerObjects.Add(worker, new JobWorkerObjects(task, cancellationTokenSource));
    }

    public JobWorker? GetJobWorker(Job job)
    {
        return _jobWorkerObjects.Keys.SingleOrDefault(w => w.Job != null && w.Job.Id == job.Id);
    }

    public void CancelJobWorkerToken(JobWorker worker)
    {
        _jobWorkerObjects[worker].CancellationTokenSource.Cancel();
    }

    public Task GetJobWorkerTask(JobWorker worker)
    {
        // return Task
        return _jobWorkerObjects[worker].Task;
    }

    private class JobWorkerObjects
    {
        public Task Task { get; set; }

        public CancellationTokenSource CancellationTokenSource { get; set; }

        public JobWorkerObjects(Task task, CancellationTokenSource cancellationTokenSource)
        {
            Task = task;
            CancellationTokenSource = cancellationTokenSource;
        }
    }
}
