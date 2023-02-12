using Microsoft.Extensions.Logging;
using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.JobWorkerServices;

public class JobWorkerService : IJobWorkerService, ITransientDependency
{
    // TODO: configure
    public int WorkerLimit => 4;

    public int AvailableWorkers => WorkerLimit - _jobWorkerCollectionService.Workers.Count;

    public bool CanStartAdditionalWorkers => AvailableWorkers > 0;

    public ILogger? Logger { get; set; }

    private readonly IJobWorkerFactory _jobWorkerFactory;
    private readonly IJobWorkerCollectionService _jobWorkerCollectionService;

    public JobWorkerService(
        IJobWorkerFactory jobWorkerFactory,
        IJobWorkerCollectionService jobWorkerCollectionService)
    {
        _jobWorkerFactory = jobWorkerFactory;
        _jobWorkerCollectionService = jobWorkerCollectionService;
    }

    public Task<bool> StartJobWorkerAsync(Job job)
    {
        if (_jobWorkerCollectionService.GetJobWorker(job) != null || !CanStartAdditionalWorkers)
        {
            return Task.FromResult(false);
        }

        var worker = _jobWorkerFactory.CreateWorker(job);
        var cancellationTokenSource = new CancellationTokenSource();
        var task = Task.Run(DoWorkAsync);

        _jobWorkerCollectionService.AddJobWorker(worker, task, cancellationTokenSource);
        return Task.FromResult(true);

        async Task DoWorkAsync()
        {
            try
            {
                await worker.WorkAsync(cancellationTokenSource.Token);
            }
            catch (OperationCanceledException) { }
            finally
            {
                // this is singleton
                _jobWorkerCollectionService.RemoveJobWorker(worker);
                worker.Dispose();
            }
        }
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
        _jobWorkerCollectionService.RemoveJobWorker(worker);
        worker.Dispose();
        return true;
    }

    public List<Job> GetJobsByPriority()
    {
        return _jobWorkerCollectionService.GetActiveJobs()
            .OrderByDescending(j => j.Priority)
            .ThenBy(j => j.Tasks.Count(t => t.IsActive))
            .ToList();
    }
}
