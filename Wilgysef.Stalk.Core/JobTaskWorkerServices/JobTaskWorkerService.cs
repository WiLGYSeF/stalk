using Wilgysef.Stalk.Core.JobTaskWorkerFactories;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Exceptions;

namespace Wilgysef.Stalk.Core.JobTaskWorkerServices;

public class JobTaskWorkerService : IJobTaskWorkerService
{
    private readonly IJobTaskManager _jobTaskManager;
    private readonly IJobTaskWorkerFactory _jobTaskWorkerFactory;
    private readonly IJobTaskWorkerCollectionService _jobTaskWorkerCollectionService;

    public JobTaskWorkerService(
        IJobTaskManager jobTaskManager,
        IJobTaskWorkerFactory jobTaskWorkerFactory,
        IJobTaskWorkerCollectionService jobTaskWorkerCollectionService)
    {
        _jobTaskManager = jobTaskManager;
        _jobTaskWorkerFactory = jobTaskWorkerFactory;
        _jobTaskWorkerCollectionService = jobTaskWorkerCollectionService;
    }

    public async Task<Task> StartJobTaskWorkerAsync(JobTask jobTask, CancellationToken jobCancellationToken)
    {
        if (_jobTaskWorkerCollectionService.GetJobTaskWorker(jobTask) != null)
        {
            throw new JobTaskActiveException();
        }

        var worker = _jobTaskWorkerFactory.CreateWorker(jobTask);
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(jobCancellationToken);
        var task = Task.Run(DoWorkAsync, jobCancellationToken);

        _jobTaskWorkerCollectionService.AddJobTaskWorker(worker, task, cancellationTokenSource);

        await _jobTaskManager.SetJobTaskActiveAsync(jobTask, cancellationTokenSource.Token);

        // return task, do not await
        return task;

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
                _jobTaskWorkerCollectionService.RemoveJobTaskWorker(worker);
            }
        }
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
