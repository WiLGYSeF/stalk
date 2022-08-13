using Wilgysef.Stalk.Core.JobWorkerFactories;
using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.Core.JobWorkerManagers;

public class JobTaskWorkerService : IJobTaskWorkerService
{
    public IReadOnlyCollection<JobTaskWorker> Workers => _jobTaskWorkers;

    private readonly List<JobTaskWorker> _jobTaskWorkers = new();
    private readonly Dictionary<JobTaskWorker, JobTaskWorkerObjects> _jobTaskWorkerObjects = new();

    private readonly IJobTaskWorkerFactory _jobTaskWorkerFactory;

    public JobTaskWorkerService(
        IJobTaskWorkerFactory jobTaskWorkerFactory)
    {
        _jobTaskWorkerFactory = jobTaskWorkerFactory;
    }

    public bool StartJobTaskWorker(JobTask jobTask)
    {
        var worker = _jobTaskWorkerFactory.CreateWorker(jobTask);
        var cancellationTokenSource = new CancellationTokenSource();

        var task = new Task(
            async () => await worker.WorkAsync(cancellationTokenSource.Token),
            cancellationTokenSource.Token,
            TaskCreationOptions.LongRunning);

        _jobTaskWorkers.Add(worker);
        _jobTaskWorkerObjects.Add(worker, new JobTaskWorkerObjects(task, cancellationTokenSource));

        task.Start();
        return true;
    }

    public async Task<bool> StopJobTaskWorker(JobTask task)
    {
        var worker = _jobTaskWorkers.SingleOrDefault(w => w.JobTask != null && w.JobTask.Id == task.Id);
        if (worker == null)
        {
            return false;
        }

        var objects = _jobTaskWorkerObjects[worker];
        objects.CancellationTokenSource.Cancel();

        await objects.Task;
        return true;
    }

    private class JobTaskWorkerObjects
    {
        public Task Task { get; set; }

        public CancellationTokenSource CancellationTokenSource { get; set; }

        public JobTaskWorkerObjects(Task task, CancellationTokenSource cancellationTokenSource)
        {
            Task = task;
            CancellationTokenSource = cancellationTokenSource;
        }
    }
}
