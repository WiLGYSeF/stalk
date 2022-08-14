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

    public async Task<bool> StartJobTaskWorkerAsync(Job job, JobTask jobTask)
    {
        if (_jobTaskWorkerCollectionService.GetJobTaskWorker(jobTask) != null)
        {
            throw new JobTaskActiveException();
        }

        var worker = _jobTaskWorkerFactory.CreateWorker(jobTask);
        var cancellationTokenSource = new CancellationTokenSource();

        var task = new Task(
            async () => await worker.WorkAsync(cancellationTokenSource.Token),
            cancellationTokenSource.Token,
            TaskCreationOptions.LongRunning);

        _jobTaskWorkerCollectionService.AddJobTaskWorker(worker, task, cancellationTokenSource);

        await _jobManager.SetJobTaskActiveAsync(job, jobTask);
        task.Start();
        
        return true;
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
}
