using System.Collections.Concurrent;
using Wilgysef.Stalk.Core.JobTaskWorkers;
using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.Core.JobTaskWorkerServices;

public class JobTaskWorkerCollectionService : IJobTaskWorkerCollectionService
{
    public IReadOnlyCollection<IJobTaskWorker> Workers => (IReadOnlyCollection<JobTaskWorker>)_jobTaskWorkers.Keys;

    private readonly ConcurrentDictionary<IJobTaskWorker, JobTaskWorkerValues> _jobTaskWorkers = new();

    public void AddJobTaskWorker(IJobTaskWorker worker, Task task, CancellationTokenSource cancellationTokenSource)
    {
        _jobTaskWorkers[worker] = new JobTaskWorkerValues(task, cancellationTokenSource);
    }

    public void RemoveJobTaskWorker(IJobTaskWorker worker)
    {
        _jobTaskWorkers.Remove(worker, out _);
    }

    public IJobTaskWorker? GetJobTaskWorker(JobTask jobTask)
    {
        return _jobTaskWorkers.Keys.SingleOrDefault(w => w.JobTask != null && w.JobTask.Id == jobTask.Id);
    }

    public void CancelJobTaskWorkerToken(IJobTaskWorker worker)
    {
        _jobTaskWorkers[worker].CancellationTokenSource.Cancel();
    }

    public Task GetJobTaskWorkerTask(IJobTaskWorker worker)
    {
        // return task
        return _jobTaskWorkers[worker].Task;
    }

    public IEnumerable<JobTask> GetActiveJobTasks()
    {
        return (IEnumerable<JobTask>)Workers
            .Select(w => w.JobTask);
    }

    private class JobTaskWorkerValues
    {
        public Task Task { get; set; }

        public CancellationTokenSource CancellationTokenSource { get; set; }

        public JobTaskWorkerValues(Task task, CancellationTokenSource cancellationTokenSource)
        {
            Task = task;
            CancellationTokenSource = cancellationTokenSource;
        }
    }
}
