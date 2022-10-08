using Wilgysef.Stalk.Core.JobWorkerServices;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Exceptions;

namespace Wilgysef.Stalk.Core.Models.Jobs;

public class JobStateManager : IJobStateManager, ITransientDependency
{
    private readonly IJobManager _jobManager;
    private readonly IJobRepository _jobRepository;
    private readonly IJobWorkerService _jobWorkerService;

    public JobStateManager(
        IJobManager jobManager,
        IJobRepository jobRepository,
        IJobWorkerService jobWorkerService)
    {
        _jobManager = jobManager;
        _jobRepository = jobRepository;
        _jobWorkerService = jobWorkerService;
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
        foreach (var jobTask in job.Tasks)
        {
            if (!jobTask.IsDone)
            {
                jobTask.ChangeState(JobTaskState.Cancelled);
            }
        }

        await _jobManager.UpdateJobAsync(job);
    }

    public async Task PauseJobAsync(Job job)
    {
        await PauseJobAsync(job, true);
        await _jobManager.UpdateJobAsync(job);
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

    public async Task PauseJobsAsync()
    {
        var jobs = await _jobManager.GetJobsAsync();

        foreach (var job in jobs)
        {
            await PauseJobAsync(job, true);
        }

        await _jobManager.UpdateJobsAsync(jobs);
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
                _jobRepository.Update(job);
            }

            await _jobWorkerService.StopJobWorkerAsync(job);
        }

        if (changeState)
        {
            job.ChangeState(JobState.Paused);
            foreach (var jobTask in job.Tasks)
            {
                if (jobTask.IsActive)
                {
                    jobTask.ChangeState(JobTaskState.Inactive);
                }
            }

            _jobRepository.Update(job);
        }
    }
}
