using Wilgysef.Stalk.Core.JobTaskWorkerFactories;
using Wilgysef.Stalk.Core.JobTaskWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Exceptions;

namespace Wilgysef.Stalk.Core.JobTaskWorkerServices;

public class JobTaskWorkerService : IJobTaskWorkerService
{
    public IReadOnlyCollection<JobTaskWorker> Workers => _jobTaskWorkerCollectionService.Workers;

    private readonly IJobManager _jobManager;
    private readonly IJobTaskWorkerFactory _jobTaskWorkerFactory;
    private readonly IJobTaskWorkerCollectionService _jobTaskWorkerCollectionService;

    public JobTaskWorkerService(
        IJobManager jobManager,
        IJobTaskWorkerFactory jobTaskWorkerFactory,
        IJobTaskWorkerCollectionService jobTaskWorkerCollectionService)
    {
        _jobManager = jobManager;
        _jobTaskWorkerFactory = jobTaskWorkerFactory;
        _jobTaskWorkerCollectionService = jobTaskWorkerCollectionService;
    }

    public async Task<Task> StartJobTaskWorkerAsync(Job job, JobTask jobTask, CancellationToken jobCancellationToken)
    {
        if (_jobTaskWorkerCollectionService.GetJobTaskWorker(jobTask) != null)
        {
            throw new JobTaskActiveException();
        }

        var worker = _jobTaskWorkerFactory.CreateWorker(jobTask);
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(jobCancellationToken);

        var task = new Task(
            async () => await DoWorkAsync(worker, cancellationTokenSource.Token),
            cancellationTokenSource.Token,
            TaskCreationOptions.LongRunning);

        _jobTaskWorkerCollectionService.AddJobTaskWorker(worker, task, cancellationTokenSource);

        await _jobManager.SetJobTaskActiveAsync(job, jobTask);
        task.Start();
        
        // return task
        return task;
    }

    public async Task<bool> StopJobTaskWorkerAsync(JobTask task)
    {
        var worker = _jobTaskWorkerCollectionService.GetJobTaskWorker(task);
        if (worker == null)
        {
            return false;
        }

        _jobTaskWorkerCollectionService.CancelJobTaskWorkerToken(worker);
        await _jobTaskWorkerCollectionService.GetJobTaskWorkerTask(worker);
        return true;
    }

    private static async Task DoWorkAsync(IJobTaskWorker worker, CancellationToken cancellationToken)
    {
        try
        {
            await worker.WorkAsync(cancellationToken);
        }
        catch (OperationCanceledException) { }
    }
}
