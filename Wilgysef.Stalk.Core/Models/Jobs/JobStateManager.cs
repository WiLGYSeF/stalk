using Wilgysef.Stalk.Core.JobWorkerManagers;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Exceptions;

namespace Wilgysef.Stalk.Core.Models.Jobs;

public class JobStateManager : IJobStateManager
{
    private readonly IJobManager _jobManager;
    private readonly IJobWorkerService _jobWorkerService;
    private readonly IJobTaskWorkerService _jobTaskWorkerService;

    public JobStateManager(
        IJobManager jobManager,
        IJobWorkerService jobWorkerService,
        IJobTaskWorkerService jobTaskWorkerService)
    {
        _jobManager = jobManager;
        _jobWorkerService = jobWorkerService;
        _jobTaskWorkerService = jobTaskWorkerService;
    }

    public async Task StopJobAsync(Job job)
    {
        if (job.IsFinished)
        {
            return;
        }
        if (job.IsTransitioning)
        {
            // TODO: take over pause with cancel
            return;
        }

        if (job.IsActive)
        {
            job.ChangeState(JobState.Cancelling);
            await _jobManager.UpdateJobAsync(job);
            await PauseJobAsync(job, false);
        }

        job.ChangeState(JobState.Cancelled);
        await _jobManager.UpdateJobAsync(job);
    }

    public async Task PauseJobAsync(Job job)
    {
        await PauseJobAsync(job, true);
    }

    public async Task UnpauseJobAsync(Job job)
    {
        if (job.IsDone)
        {
            throw new JobAlreadyDoneException();
        }
        if (job.IsActive)
        {
            return;
        }

        job.ChangeState(JobState.Inactive);
        await _jobManager.UpdateJobAsync(job);
    }

    public async Task StopJobTaskAsync(Job job, JobTask task)
    {
        if (task.IsFinished)
        {
            return;
        }
        if (task.IsTransitioning)
        {
            // TODO: take over pause with cancel
            return;
        }

        if (task.IsActive)
        {
            task.ChangeState(JobTaskState.Cancelling);
            await _jobManager.UpdateJobAsync(job);
            await PauseJobTaskAsync(job, task, false);
        }

        task.ChangeState(JobTaskState.Cancelled);
        await _jobManager.UpdateJobAsync(job);
    }

    public async Task PauseJobTaskAsync(Job job, JobTask task)
    {
        await PauseJobTaskAsync(job, task, true);
    }

    public async Task UnpauseJobTaskAsync(Job job, JobTask task)
    {
        if (task.IsDone)
        {
            throw new JobTaskAlreadyDoneException();
        }
        if (task.IsActive)
        {
            return;
        }

        task.ChangeState(JobTaskState.Inactive);
        await _jobManager.UpdateJobAsync(job);
    }

    private async Task PauseJobAsync(Job job, bool changeState)
    {
        if (job.IsDone || job.IsTransitioning)
        {
            return;
        }

        if (job.IsActive)
        {
            // TODO: restore state

            if (changeState)
            {
                job.ChangeState(JobState.Pausing);
                await _jobManager.UpdateJobAsync(job);
            }

            await _jobWorkerService.StopJobWorker(job);
        }

        if (changeState)
        {
            job.ChangeState(JobState.Paused);
            await _jobManager.UpdateJobAsync(job);
        }
    }

    private async Task PauseJobTaskAsync(Job job, JobTask task, bool changeState)
    {
        if (task.IsDone || task.IsTransitioning)
        {
            return;
        }

        if (task.IsActive)
        {
            // TODO: restore state

            if (changeState)
            {
                task.ChangeState(JobTaskState.Pausing);
                await _jobManager.UpdateJobAsync(job);
            }

            await _jobTaskWorkerService.StopJobTaskWorker(task);
        }

        if (changeState)
        {
            task.ChangeState(JobTaskState.Paused);
            await _jobManager.UpdateJobAsync(job);
        }
    }
}
