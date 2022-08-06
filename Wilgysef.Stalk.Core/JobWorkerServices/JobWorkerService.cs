using Wilgysef.Stalk.Core.JobWorkerFactories;
using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Core.JobWorkerManagers;

public class JobWorkerService : IJobWorkerService
{
    public IReadOnlyCollection<JobWorker> Workers => _jobWorkers;

    private int WorkerLimit { get; set; } = 4;

    private readonly List<JobWorker> _jobWorkers = new();
    private readonly Dictionary<JobWorker, JobWorkerObjects> _jobWorkerObjects = new();

    private readonly IJobWorkerFactory _jobWorkerFactory;

    public JobWorkerService(
        IJobWorkerFactory jobWorkerFactory)
    {
        _jobWorkerFactory = jobWorkerFactory;
    }

    public bool StartJobWorker(Job job)
    {
        if (_jobWorkers.Count >= WorkerLimit)
        {
            return false;
        }

        var worker = _jobWorkerFactory.CreateWorker(job);
        var cancellationTokenSource = new CancellationTokenSource();

        var token = cancellationTokenSource.Token;
        var task = new Task(async () => await worker.WorkAsync(token), token, TaskCreationOptions.LongRunning);

        _jobWorkers.Add(worker);
        _jobWorkerObjects.Add(worker, new JobWorkerObjects(task, cancellationTokenSource));

        task.Start();
        return true;
    }

    public async Task<bool> StopJobWorker(Job job, bool awaitTask)
    {
        var worker = _jobWorkers.SingleOrDefault(w => w.Job != null && w.Job.Id == job.Id);
        if (worker == null)
        {
            return false;
        }

        var objects = _jobWorkerObjects[worker];
        objects.CancellationTokenSource.Cancel();

        if (awaitTask)
        {
            await objects.Task;
        }
        return true;
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
