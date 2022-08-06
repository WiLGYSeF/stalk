using Wilgysef.Stalk.Core.JobWorkerManagers;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Exceptions;

namespace Wilgysef.Stalk.Core.Models.Jobs;

public class JobStateManager : IJobStateManager
{
    private readonly IJobManager _jobManager;
    private readonly IJobWorkerManager _jobWorkerManager;

    public JobStateManager(
        IJobManager jobManager,
        IJobWorkerManager jobWorkerManager)
    {
        _jobManager = jobManager;
        _jobWorkerManager = jobWorkerManager;
    }

    public async Task StopJobAsync(Job job, bool blocking = false)
    {
        await StopJobNoSaveAsync(job, blocking);
        await _jobManager.UpdateJobAsync(job);
    }

    public async Task PauseJobAsync(Job job, bool blocking = false)
    {
        await PauseJobNoSaveAsync(job, blocking);
        await _jobManager.UpdateJobAsync(job);
    }

    public async Task UnpauseJobAsync(Job job)
    {
        if (job.State != JobState.Paused)
        {
            throw new JobNotPausedException();
        }

        job.ChangeState(JobState.Inactive);

        await _jobManager.UpdateJobAsync(job);
    }

    public async Task StopJobTaskAsync(Job job, JobTask task, bool blocking = false)
    {
        await StopJobTaskNoSaveAsync(job, task, blocking);
        await _jobManager.UpdateJobAsync(job);
    }

    public async Task PauseJobTaskAsync(Job job, JobTask task, bool blocking = false)
    {
        await PauseJobTaskNoSaveAsync(job, task, blocking);
        await _jobManager.UpdateJobAsync(job);
    }

    public async Task UnpauseJobTaskAsync(Job job, JobTask task)
    {
        if (task.State != JobTaskState.Paused)
        {
            throw new JobTaskNotPausedException();
        }

        task.ChangeState(JobTaskState.Inactive);

        await _jobManager.UpdateJobAsync(job);
    }

    private async Task StopJobNoSaveAsync(Job job, bool blocking = false)
    {
        if (job.IsDone)
        {
            throw new JobAlreadyDoneException();
        }

        if (job.IsActive)
        {
            await PauseJobNoSaveAsync(job, blocking);

            job.ChangeState(JobState.Cancelled);
        }
    }

    private async Task PauseJobNoSaveAsync(Job job, bool blocking = false)
    {
        if (job.IsDone)
        {
            throw new JobAlreadyDoneException();
        }

        if (job.IsActive)
        {
            await _jobWorkerManager.StopJobWorker(job, blocking);

            job.ChangeState(JobState.Paused);
        }
    }

    private async Task StopJobTaskNoSaveAsync(Job job, JobTask task, bool blocking = false)
    {
        if (task.IsDone)
        {
            throw new JobTaskAlreadyDoneException();
        }

        if (task.IsActive)
        {
            await PauseJobTaskNoSaveAsync(job, task, blocking);

            task.ChangeState(JobTaskState.Cancelled);
            task.Finish();
        }
    }

    private async Task PauseJobTaskNoSaveAsync(Job job, JobTask task, bool blocking = false)
    {
        if (task.IsDone)
        {
            throw new JobTaskAlreadyDoneException();
        }

        if (task.IsActive)
        {
            // TODO: stop job task
            // TODO: nonblock?

            task.ChangeState(JobTaskState.Paused);
        }
    }
}
