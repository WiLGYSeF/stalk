﻿using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Exceptions;

namespace Wilgysef.Stalk.Core.Models.Jobs;

public class JobManager : IJobManager
{
    private readonly IUnitOfWork _unitOfWork;

    public JobManager(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Job> CreateJobAsync(Job job)
    {
        var entity = await _unitOfWork.JobRepository.AddAsync(job);
        await _unitOfWork.SaveChangesAsync();
        return entity;
    }

    public async Task<Job> GetJobAsync(long id)
    {
        var entity = await _unitOfWork.JobRepository.FindAsync(id);
        if (entity == null)
        {
            throw new EntityNotFoundException(nameof(Job), id);
        }

        return entity;
    }

    public async Task<List<Job>> GetJobsAsync()
    {
        return await _unitOfWork.JobRepository.GetJobs()
            .OrderBy(j => j.Id)
            .ToListAsync();
    }

    public async Task<List<Job>> GetUnfinishedJobsAsync()
    {
        return await _unitOfWork.JobRepository.GetJobs()
            .Where(Expression.Lambda<Func<Job, bool>>(Expression.Negate(Job.IsDoneExpression)))
            .ToListAsync();
    }

    public async Task<Job?> GetNextPriorityJobAsync()
    {
        return await _unitOfWork.JobRepository.GetJobs()
            .Where(Job.IsQueuedExpression)
            .Where(j => j.Tasks.AsQueryable()
                .Any(JobTask.IsQueuedExpression))
            .OrderByDescending(j => j.Priority)
            .ThenBy(j => j.Started)
            .FirstOrDefaultAsync();
    }

    public async Task<Job> UpdateJobAsync(Job job)
    {
        var entity = _unitOfWork.JobRepository.Update(job);
        await _unitOfWork.SaveChangesAsync();
        return entity;
    }

    public async Task DeleteJobAsync(Job job)
    {
        if (job.IsActive)
        {
            throw new JobActiveException();
        }

        _unitOfWork.JobRepository.Remove(job);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task SetJobActiveAsync(Job job)
    {
        job.ChangeState(JobState.Active);

        _unitOfWork.JobRepository.Update(job);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task SetJobDoneAsync(Job job)
    {
        job.ChangeState(!job.HasUnfinishedTasks
            ? JobState.Completed
            : JobState.Failed);

        _unitOfWork.JobRepository.Update(job);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeactivateJobsAsync()
    {
        var jobs = await _unitOfWork.JobRepository.GetJobs().ToListAsync();

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

        _unitOfWork.JobRepository.UpdateRange(jobs);
        await _unitOfWork.SaveChangesAsync();
    }
}
