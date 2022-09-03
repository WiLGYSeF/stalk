using Wilgysef.Stalk.Core.JobTaskWorkerServices;
using Wilgysef.Stalk.Core.JobWorkerServices;
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

            await _jobWorkerService.StopJobWorkerAsync(job);
        }

        if (changeState)
        {
            job.ChangeState(JobState.Paused);
            await _jobManager.UpdateJobAsync(job);
        }
    }
}
