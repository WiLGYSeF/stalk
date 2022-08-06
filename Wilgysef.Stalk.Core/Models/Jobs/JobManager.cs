using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
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
        var entity = await GetJobs()
            .Where(j => j.Id == id)
            .SingleOrDefaultAsync();
        if (entity == null)
        {
            throw new EntityNotFoundException(nameof(Job), id);
        }

        return entity;
    }

    public async Task<List<Job>> GetJobsAsync()
    {
        return await GetJobs().ToListAsync();
    }

    public async Task<List<Job>> GetUnfinishedJobsAsync()
    {
        return await GetJobs()
            .Where(Expression.Lambda<Func<Job, bool>>(Expression.Negate(Job.IsDoneExpression)))
            .ToListAsync();
    }

    public async Task<Job?> GetNextPriorityJobAsync()
    {
        return await GetJobs()
            .Where(Job.IsQueuedExpression)
            .Where(j => j.Tasks.AsQueryable()
                .Any(JobTask.IsQueuedExpression))
            .OrderByDescending(j => j.Priority)
            .ThenBy(j => j.Started)
            .FirstOrDefaultAsync();
    }

    public async Task<Job> UpdateJobAsync(Job job)
    {
        var entity = _dbContext.Jobs.Update(job);
        await _dbContext.SaveChangesAsync();
        return entity.Entity;
    }

    public async Task DeleteJobAsync(Job job)
    {
        if (job.IsActive)
        {
            throw new JobActiveException();
        }

        _dbContext.Jobs.Remove(job);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteJobTaskAsync(JobTask task)
    {
        if (task.IsActive)
        {
            throw new JobTaskActiveException();
        }

        _dbContext.JobTasks.Remove(task);
        await _dbContext.SaveChangesAsync();
    }

    public async Task SetJobActiveAsync(Job job)
    {
        job.ChangeState(JobState.Active);

        _dbContext.Jobs.Update(job);
        await _dbContext.SaveChangesAsync();
    }

    public async Task SetJobDoneAsync(Job job)
    {
        job.ChangeState(!job.HasUnfinishedTasks
            ? JobState.Completed
            : JobState.Failed);

        _dbContext.Jobs.Update(job);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeactivateJobsAsync()
    {
        var jobs = await GetJobs().ToListAsync();

        foreach (var job in jobs)
        {
            switch (job.State)
            {
                case JobState.Active:
                    job.ChangeState(JobState.Inactive);
                    break;
                case JobState.Cancelling:
                    job.ChangeState(JobState.Cancelled);
                    break;
                case JobState.Pausing:
                    job.ChangeState(JobState.Paused);
                    break;
                default:
                    break;
            }
        }

        _dbContext.Jobs.UpdateRange(jobs);
        await _dbContext.SaveChangesAsync();
    }

    private IQueryable<Job> GetJobs()
    {
        return _dbContext.Jobs
            .Include(j => j.Tasks);
    }
}
