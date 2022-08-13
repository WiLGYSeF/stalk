using Wilgysef.Stalk.Core.JobWorkerFactories;
using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Exceptions;

namespace Wilgysef.Stalk.Core.JobWorkerManagers;

public class JobWorkerService : IJobWorkerService
{
    public IReadOnlyCollection<JobWorker> Workers => _jobWorkers;

    public IReadOnlyCollection<Job> Jobs => (IReadOnlyCollection<Job>)Workers
        .Select(w => w.Job)
        .Where(j => j != null)
        .ToList();

    public bool CanStartAdditionalWorkers => _jobWorkers.Count < WorkerLimit;

    private int WorkerLimit { get; set; } = 4;

    private readonly List<JobWorker> _jobWorkers = new();
    private readonly Dictionary<JobWorker, JobWorkerObjects> _jobWorkerObjects = new();

    private readonly IJobManager _jobManager;
    private readonly IJobWorkerFactory _jobWorkerFactory;

    public JobWorkerService(
        IJobManager jobManager,
        IJobWorkerFactory jobWorkerFactory)
    {
        _jobManager = jobManager;
        _jobWorkerFactory = jobWorkerFactory;
    }

    public async Task<bool> StartJobWorker(Job job)
    {
        if (_jobWorkers.Any(j => j.Job != null && j.Job.Id == job.Id))
        {
            throw new JobActiveException();
        }

        if (!CanStartAdditionalWorkers)
        {
            return false;
        }

        var worker = _jobWorkerFactory.CreateWorker(job);
        var cancellationTokenSource = new CancellationTokenSource();

        var token = cancellationTokenSource.Token;
        var task = new Task(async () => await worker.WorkAsync(token), token, TaskCreationOptions.LongRunning);

        _jobWorkers.Add(worker);
        _jobWorkerObjects.Add(worker, new JobWorkerObjects(task, cancellationTokenSource));

        await _jobManager.SetJobActiveAsync(job);
        task.Start();

        return true;
    }

    public async Task<bool> StopJobWorker(Job job)
    {
        var worker = _jobWorkers.SingleOrDefault(w => w.Job != null && w.Job.Id == job.Id);
        if (worker == null)
        {
            return false;
        }

        var objects = _jobWorkerObjects[worker];
        objects.CancellationTokenSource.Cancel();

        await objects.Task;
        return true;
    }

    public IReadOnlyList<Job> GetJobsByPriority()
    {
        return Jobs
            .OrderByDescending(j => j.Priority)
            .ThenBy(j => j.Tasks.Count(t => t.IsActive))
            .ToList();
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
