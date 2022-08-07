using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Exceptions;
using Wilgysef.Stalk.Core.Specifications;

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
        var entity = await _unitOfWork.JobRepository.FirstOrDefaultAsync(new JobSingleSpecification(id));
        if (entity == null)
        {
            throw new EntityNotFoundException(nameof(Job), id);
        }

        return entity;
    }

    public async Task<List<Job>> GetJobsAsync()
    {
        return await _unitOfWork.JobRepository.ListAsync();
    }

    public async Task<List<Job>> GetJobsAsync(ISpecification<Job> specification)
    {
        return await _unitOfWork.JobRepository.ListAsync(specification);
    }

    public async Task<Job?> GetNextPriorityJobAsync()
    {
        return await _unitOfWork.JobRepository.FirstOrDefaultAsync(new QueuedJobsSpecification());
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
        var jobs = await _unitOfWork.JobRepository.ListAsync();

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
