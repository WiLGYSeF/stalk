using Wilgysef.Stalk.Core.Exceptions;
using Wilgysef.Stalk.Core.JobTaskWorkerServices;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.Enums;

namespace Wilgysef.Stalk.Core.Models.JobTasks;

public class JobTaskStateManager : IJobTaskStateManager, ITransientDependency
{
    private readonly IJobTaskManager _jobTaskManager;
    private readonly IJobTaskWorkerService _jobTaskWorkerService;

    public JobTaskStateManager(
        IJobTaskManager jobTaskManager,
        IJobTaskWorkerService jobTaskWorkerService)
    {
        _jobTaskManager = jobTaskManager;
        _jobTaskWorkerService = jobTaskWorkerService;
    }

    public async Task StopJobTaskAsync(JobTask jobTask)
    {
        CheckJobTaskNotTransitioning(jobTask);

        if (jobTask.IsDone)
        {
            return;
        }

        if (jobTask.IsActive)
        {
            jobTask.ChangeState(JobTaskState.Cancelling);
            await _jobTaskManager.UpdateJobTaskAsync(jobTask);
            await PauseJobTaskAsync(jobTask, false);
        }

        jobTask.ChangeState(JobTaskState.Cancelled);
        await _jobTaskManager.UpdateJobTaskAsync(jobTask);
    }

    public async Task PauseJobTaskAsync(JobTask jobTask)
    {
        CheckJobTaskNotTransitioning(jobTask);

        if (jobTask.IsDone)
        {
            return;
        }

        await PauseJobTaskAsync(jobTask, true);
    }

    public async Task UnpauseJobTaskAsync(JobTask jobTask)
    {
        CheckJobTaskNotTransitioning(jobTask);

        if (jobTask.IsDone)
        {
            throw new JobTaskAlreadyDoneException();
        }
        if (jobTask.IsActive)
        {
            return;
        }

        jobTask.ChangeState(JobTaskState.Inactive);
        await _jobTaskManager.UpdateJobTaskAsync(jobTask);
    }

    private async Task PauseJobTaskAsync(JobTask jobTask, bool changeState)
    {
        if (jobTask.IsActive)
        {
            if (changeState)
            {
                jobTask.ChangeState(JobTaskState.Pausing);
                await _jobTaskManager.UpdateJobTaskAsync(jobTask);
            }

            await _jobTaskWorkerService.StopJobTaskWorkerAsync(jobTask);
        }

        if (changeState)
        {
            jobTask.ChangeState(JobTaskState.Paused);
            await _jobTaskManager.UpdateJobTaskAsync(jobTask);
        }
    }

    private static void CheckJobTaskNotTransitioning(JobTask jobTask)
    {
        if (jobTask.IsTransitioning)
        {
            throw new JobTaskTransitioningException();
        }
    }
}
