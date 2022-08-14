using Wilgysef.Stalk.Core.JobWorkerFactories;
using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Exceptions;

namespace Wilgysef.Stalk.Core.JobWorkerServices;

public class JobWorkerService : IJobWorkerService
{
    public bool CanStartAdditionalWorkers => _jobWorkerCollectionService.Workers.Count < WorkerLimit;

    private int WorkerLimit { get; set; } = 4;

    private readonly IJobManager _jobManager;
    private readonly IJobWorkerFactory _jobWorkerFactory;
    private readonly IJobWorkerCollectionService _jobWorkerCollectionService;

    public JobWorkerService(
        IJobManager jobManager,
        IJobWorkerFactory jobWorkerFactory,
        IJobWorkerCollectionService jobWorkerCollectionService)
    {
        _jobManager = jobManager;
        _jobWorkerFactory = jobWorkerFactory;
        _jobWorkerCollectionService = jobWorkerCollectionService;
    }

    public async Task<bool> StartJobWorkerAsync(Job job)
    {
        if (_jobWorkerCollectionService.GetJobWorker(job) != null)
        {
            throw new JobActiveException();
        }

        if (!CanStartAdditionalWorkers)
        {
            return false;
        }

        var worker = _jobWorkerFactory.CreateWorker(job);
        var cancellationTokenSource = new CancellationTokenSource();

        var task = new Task(
            async () => await worker.WorkAsync(cancellationTokenSource.Token),
            cancellationTokenSource.Token,
            TaskCreationOptions.LongRunning);

        _jobWorkerCollectionService.AddJobWorker(worker, task, cancellationTokenSource);

        await _jobManager.SetJobActiveAsync(job);
        task.Start();

        return true;
    }

    public async Task<bool> StopJobWorkerAsync(Job job)
    {
        var worker = _jobWorkerCollectionService.GetJobWorker(job);
        if (worker == null)
        {
            return false;
        }

        _jobWorkerCollectionService.CancelJobWorkerToken(worker);
        await _jobWorkerCollectionService.GetJobWorkerTask(worker);
        return true;
    }

    public List<Job> GetJobsByPriority()
    {
        return _jobWorkerCollectionService.Jobs
            .OrderByDescending(j => j.Priority)
            .ThenBy(j => j.Tasks.Count(t => t.IsActive))
            .ToList();
    }
}
