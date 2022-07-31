using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Exceptions;

namespace Wilgysef.Stalk.Core.Models.Jobs;

public class JobManager : IJobManager
{
    private readonly IStalkDbContext _dbContext;

    public JobManager(
        IStalkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Job> CreateJobAsync(Job job)
    {
        var entity = (await _dbContext.Jobs.AddAsync(job)).Entity;
        await _dbContext.SaveChangesAsync();
        return entity;
    }

    public async Task<Job> GetJobAsync(long id)
    {
        // TODO: Include()?
        var entity = await _dbContext.Jobs.FindAsync(id);
        if (entity == null)
        {
            throw new EntityNotFoundException(nameof(Job), id);
        }

        return entity;
    }

    public async Task<Job> UpdateJobAsync(Job job)
    {
        var entity = _dbContext.Jobs.Update(job);
        await _dbContext.SaveChangesAsync();
        return entity.Entity;
    }

    public async Task DeleteJobAsync(Job job, bool force = false)
    {
        await StopJobNoSaveAsync(job, force);

        _dbContext.Jobs.Remove(job);
        await _dbContext.SaveChangesAsync();
    }

    public async Task StopJobAsync(Job job, bool force = false)
    {
        await StopJobNoSaveAsync(job, force);

        _dbContext.Jobs.Update(job);
        await _dbContext.SaveChangesAsync();
    }

    public async Task PauseJobAsync(Job job, bool force = false)
    {
        await PauseJobNoSaveAsync(job, force);

        _dbContext.Jobs.Update(job);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteJobTaskAsync(Job job, JobTask task, bool force = false)
    {
        await StopJobTaskNoSaveAsync(job, task, force);

        _dbContext.JobTasks.Remove(task);
        await _dbContext.SaveChangesAsync();
    }

    public async Task StopJobTaskAsync(Job job, JobTask task, bool force = false)
    {
        await StopJobTaskNoSaveAsync(job, task, force);

        _dbContext.JobTasks.Update(task);
        await _dbContext.SaveChangesAsync();
    }

    public async Task PauseJobTaskAsync(Job job, JobTask task, bool force = false)
    {
        await PauseJobTaskNoSaveAsync(job, task, force);

        _dbContext.JobTasks.Update(task);
        await _dbContext.SaveChangesAsync();
    }

    private async Task StopJobNoSaveAsync(Job job, bool force = false)
    {
        if (job.IsActive)
        {
            await PauseJobNoSaveAsync(job, force);

            job.ChangeState(JobState.Cancelled);
            job.Finish();
        }
    }

    private async Task PauseJobNoSaveAsync(Job job, bool force = false)
    {
        if (job.IsActive)
        {
            // TODO: stop job
            // TODO: nonblock?

            job.ChangeState(JobState.Paused);
        }
    }

    private async Task StopJobTaskNoSaveAsync(Job job, JobTask task, bool force = false)
    {
        if (task.IsActive)
        {
            await PauseJobTaskNoSaveAsync(job, task, force);

            task.ChangeState(JobTaskState.Cancelled);
            task.Finish();
        }
    }

    private async Task PauseJobTaskNoSaveAsync(Job job, JobTask task, bool force = false)
    {
        if (task.IsActive)
        {
            // TODO: stop job task
            // TODO: nonblock?

            task.ChangeState(JobTaskState.Paused);
        }
    }
}
