using Wilgysef.Stalk.Core.JobTaskWorkerServices;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Exceptions;

namespace Wilgysef.Stalk.Core.Models.JobTasks;

public class JobTaskStateManager : IJobTaskStateManager
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
        if (jobTask.IsFinished)
        {
            return;
        }
        if (jobTask.IsTransitioning)
        {
            // TODO: take over pause with cancel
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
        await PauseJobTaskAsync(jobTask, true);
    }

    public async Task UnpauseJobTaskAsync(JobTask jobTask)
    {
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
        if (jobTask.IsDone || jobTask.IsTransitioning)
        {
            return;
        }

        if (jobTask.IsActive)
        {
            // TODO: restore state

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
}
