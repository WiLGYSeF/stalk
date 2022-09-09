using Ardalis.Specification;
using Wilgysef.Stalk.Core.DomainEvents.Events;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Exceptions;
using Wilgysef.Stalk.Core.Specifications;

namespace Wilgysef.Stalk.Core.Models.Jobs;

public class JobManager : IJobManager, ITransientDependency
{
    private readonly IUnitOfWork _unitOfWork;

    public JobManager(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Job> CreateJobAsync(Job job, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.JobRepository.AddAsync(job, cancellationToken);

        job.DomainEvents.RemoveType<JobStateChangedEvent>();
        job.DomainEvents.RemoveType<JobPriorityChangedEvent>();
        job.DomainEvents.AddOrReplace(new JobCreatedEvent(job.Id));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<Job> GetJobAsync(long id, CancellationToken cancellationToken = default)
    {
        return await GetJobAsync(id, false, cancellationToken);
    }

    public async Task<Job> GetJobAsync(long id, bool readOnly, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.JobRepository.FirstOrDefaultAsync(
            new JobSingleSpecification(jobId: id, readOnly: readOnly),
            cancellationToken);
        if (entity == null)
        {
            throw new EntityNotFoundException(nameof(Job), id);
        }

        return entity;
    }

    public async Task<Job> GetJobByTaskIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.JobRepository.FirstOrDefaultAsync(
            new JobSingleSpecification(taskId: id),
            cancellationToken);
        if (entity == null)
        {
            throw new EntityNotFoundException(nameof(Job), id);
        }

        return entity;
    }

    public async Task<List<Job>> GetJobsAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.JobRepository.ListAsync(cancellationToken);
    }

    public async Task<List<Job>> GetJobsAsync(ISpecification<Job> specification, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.JobRepository.ListAsync(specification, cancellationToken);
    }

    public async Task<Job?> GetNextPriorityJobAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.JobRepository.FirstOrDefaultAsync(
            new QueuedJobsSpecification(),
            cancellationToken);
    }

    public async Task<Job> UpdateJobAsync(Job job, CancellationToken cancellationToken = default)
    {
        // TODO: change this?
        _unitOfWork.JobRepository.Update(job);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return job;
    }

    public async Task DeleteJobAsync(Job job, CancellationToken cancellationToken = default)
    {
        if (job.IsActive)
        {
            throw new JobActiveException();
        }

        _unitOfWork.JobRepository.Remove(job);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetJobActiveAsync(Job job, CancellationToken cancellationToken = default)
    {
        if (job.IsActive)
        {
            return;
        }

        job.ChangeState(JobState.Active);

        await UpdateJobAsync(job, cancellationToken);
    }

    public async Task DeactivateJobsAsync(CancellationToken cancellationToken = default)
    {
        var jobs = await _unitOfWork.JobRepository.ListAsync(cancellationToken);

        foreach (var job in jobs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            job.Deactivate();

            foreach (var task in job.Tasks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                task.Deactivate();
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
